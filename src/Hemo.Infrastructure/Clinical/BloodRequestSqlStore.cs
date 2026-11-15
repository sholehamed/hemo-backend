using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Clinical;
using Hemo.Application.Cqrs;
using Hemo.Domain.Clinical;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Clinical;

/// <summary>Shared SQL workflow for nurse request, physician review and bank worklist.</summary>
public sealed class BloodRequestSqlStore(
    HemoDbContext db, HemoAccessService roles, HemoAuditWriter audit) : IBloodRequestStore
{
    private sealed record Row(BloodRequest Request, BloodRequestItem Item, string ProductCode);

    private IQueryable<Row> Rows(long facility, long department, long? requestId = null,
        string? status = null) =>
        from request in db.BloodRequests
        join encounter in db.Encounters on request.EncounterId equals encounter.EncounterId
        join location in db.EncounterLocations on request.EncounterId equals location.EncounterId
        join item in db.BloodRequestItems on request.BloodRequestId equals item.BloodRequestId
        join product in db.BloodProductTypes on item.ProductTypeId equals product.ProductTypeId
        where encounter.FacilityId == facility
            && location.DepartmentId == department && location.ToAt == null
            && (requestId == null || request.BloodRequestId == requestId)
            && (status == null || request.Status == status)
        orderby request.RequestedAt descending, request.BloodRequestId descending
        select new Row(request, item, product.Code);

    private static BloodRequestView View(Row row, long facility, long department) =>
        new(row.Request.BloodRequestId, row.Request.RequestNo, row.Request.EncounterId,
            facility, department, row.Item.ProductTypeId, row.ProductCode,
            row.Item.RequestedQuantity, row.Request.Priority, row.Request.Status,
            row.Request.Indication ?? "", row.Request.Diagnosis, row.Request.RequestedByUserId,
            row.Request.RequestedAt, Convert.ToBase64String(row.Request.RowVersion));

    public async Task<IReadOnlyList<ProductTypeOption>> ProductTypesAsync(CancellationToken ct) =>
        await db.BloodProductTypes.AsNoTracking().Where(x=>x.IsActive)
            .OrderBy(x=>x.Name).Select(x=>new ProductTypeOption(x.ProductTypeId,x.Code,x.Name))
            .ToArrayAsync(ct);

    public async Task<BloodRequestPage> ListAsync(long facility, long department,
        int page, string? status, CancellationToken ct)
    {
        page=Math.Clamp(page,1,10000);
        if (status is {Length: > 35}) status="invalid";
        var q=Rows(facility,department,status: string.IsNullOrWhiteSpace(status)?null:status).AsNoTracking();
        var total=await q.CountAsync(ct);
        var rows=await q.Skip((page-1)*20).Take(20).ToArrayAsync(ct);
        return new BloodRequestPage(rows.Select(x=>View(x,facility,department)).ToArray(),
            page,20,total);
    }

    public async Task<BloodRequestView?> GetAsync(long facility, long department,
        long requestId, CancellationToken ct)
    {
        var row=await Rows(facility,department,requestId).AsNoTracking()
            .SingleOrDefaultAsync(ct);
        return row is null ? null : View(row,facility,department);
    }

    private async Task<bool> HasScopedClinicalRoleAsync(long actor, long facility,
        long department, string role, CancellationToken ct) =>
        (await roles.GetGrantsAsync(actor,ct)).Any(x=>x.RoleCode==role
            && x.FacilityId==facility
            && (x.DepartmentId is null || x.DepartmentId==department));

    public async Task<Outcome<BloodRequestView>> SubmitAsync(long actor,long facility,
        long department,NewBloodRequest input,CancellationToken ct)
    {
        if (input.EncounterId<=0 || input.ProductTypeId<=0 || input.Quantity<=0 ||
            input.Quantity>999999999.999m || decimal.Round(input.Quantity,3)!=input.Quantity ||
            input.Priority is not ("Routine" or "Urgent" or "Emergency") ||
            input.Indication is null || input.Indication.Trim().Length is < 3 or > 2000 ||
            (input.Diagnosis?.Length??0)>1000 ||
            (input.SpecialRequirements?.Length??0)>800 ||
            input.Hemoglobin is < 0 or > 30)
            return Outcome<BloodRequestView>.Fail("invalid_request");

        var doctor=await HasScopedClinicalRoleAsync(actor,facility,department,"DOCTOR",ct);
        var nurse=await HasScopedClinicalRoleAsync(actor,facility,department,"NURSE",ct);
        if (!doctor && !nurse)
            return Outcome<BloodRequestView>.Fail("clinical_role_required");

        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var encounterValid=await (
            from enc in db.Encounters
            join loc in db.EncounterLocations on enc.EncounterId equals loc.EncounterId
            where enc.EncounterId==input.EncounterId && enc.FacilityId==facility
                && enc.Status=="Admitted" && loc.DepartmentId==department && loc.ToAt==null
            select enc.EncounterId).AnyAsync(ct);
        if(!encounterValid)return Outcome<BloodRequestView>.Fail("encounter_not_found");
        if(!await db.IdentityVerifications.AnyAsync(x=>x.EncounterId==input.EncounterId
            && x.Status=="Verified",ct))
            return Outcome<BloodRequestView>.Fail("identity_not_verified");
        if(!await db.BloodProductTypes.AnyAsync(x=>x.ProductTypeId==input.ProductTypeId
            && x.IsActive,ct))
            return Outcome<BloodRequestView>.Fail("invalid_product");

        var now=DateTime.UtcNow;
        var request=new BloodRequest
        {
            EncounterId=input.EncounterId,
            RequestNo="REQ-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),
            RequestedByUserId=actor, RequestedAt=now,
            RequestSource=doctor?"Doctor":"Nurse",RequestType="Transfusion",
            Priority=input.Priority, Status="AwaitingDoctor",
            Indication=input.Indication.Trim(),Diagnosis=input.Diagnosis?.Trim()
        };
        db.BloodRequests.Add(request);
        await db.SaveChangesAsync(ct);
        db.BloodRequestItems.Add(new BloodRequestItem
        {
            BloodRequestId=request.BloodRequestId,
            ProductTypeId=input.ProductTypeId,RequestedQuantity=input.Quantity,
            QuantityUnit="Unit",SpecialRequirements=input.SpecialRequirements?.Trim(),
            Status="Pending"
        });
        if(input.Hemoglobin.HasValue||input.PreviousReaction.HasValue)
            db.RequestClinicalData.Add(new RequestClinicalData
            {
                BloodRequestId=request.BloodRequestId,Hemoglobin=input.Hemoglobin,
                HasPreviousReaction=input.PreviousReaction
            });
        var doctorRoleId=await db.Roles.AsNoTracking()
            .Where(x=>x.Code=="DOCTOR").Select(x=>x.RoleId).SingleAsync(ct);
        db.ClinicalTasks.Add(new ClinicalTask
        {
            FacilityId=facility,EncounterId=input.EncounterId,
            BloodRequestId=request.BloodRequestId,AssignedRoleId=doctorRoleId,
            TaskType="ReviewBloodRequest",Status="Open",Priority=input.Priority,
            Summary="Review blood request "+request.RequestNo,CreatedByUserId=actor
        });
        audit.Record(actor,facility,"BloodRequest",request.BloodRequestId,"Submitted");
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome<BloodRequestView>.Ok((await GetAsync(
            facility,department,request.BloodRequestId,ct))!);
    }

    public async Task<Outcome<BloodRequestView>> ReviewAsync(long actor,long facility,
        long department,long requestId,ReviewBloodRequest input,CancellationToken ct)
    {
        if(input.Decision is not ("Approved" or "Rejected" or "Returned") ||
            (input.Reason?.Length??0)>1500 ||
            (input.Decision!="Approved" && string.IsNullOrWhiteSpace(input.Reason)))
            return Outcome<BloodRequestView>.Fail("invalid_review");
        if(!await HasScopedClinicalRoleAsync(actor,facility,department,"DOCTOR",ct))
            return Outcome<BloodRequestView>.Fail("doctor_required");

        byte[] expected;
        try{expected=Convert.FromBase64String(input.RowVersion??"");}
        catch(FormatException){return Outcome<BloodRequestView>.Fail("invalid_rowversion");}

        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await Rows(facility,department,requestId).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<BloodRequestView>.Fail("not_found");
        if(row.Request.RequestedByUserId==actor)
            return Outcome<BloodRequestView>.Fail("self_approval_prohibited");
        if(row.Request.Status!="AwaitingDoctor")
            return Outcome<BloodRequestView>.Fail("invalid_state");
        if(expected.Length!=row.Request.RowVersion.Length ||
            !CryptographicOperations.FixedTimeEquals(expected,row.Request.RowVersion))
            return Outcome<BloodRequestView>.Fail("concurrency_conflict");

        var decision=input.Decision;
        row.Request.Status=decision switch
        {
            "Approved"=>"Approved", "Rejected"=>"Rejected", _=>"Draft"
        };
        var now=DateTime.UtcNow;
        db.RequestApprovals.Add(new RequestApproval
        {
            BloodRequestId=row.Request.BloodRequestId,ReviewerUserId=actor,
            ApprovalStage="Doctor",Decision=decision,DecidedAt=now,
            Reason=input.Reason?.Trim()
        });
        var tasks=await db.ClinicalTasks.Where(x=>x.BloodRequestId==requestId
            && x.TaskType=="ReviewBloodRequest" && x.Status=="Open").ToArrayAsync(ct);
        foreach(var task in tasks){task.Status="Completed";task.CompletedAt=now;}
        audit.Record(actor,facility,"BloodRequest",requestId,decision);
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<BloodRequestView>.Fail("concurrency_conflict");}
        return Outcome<BloodRequestView>.Ok((await GetAsync(facility,department,requestId,ct))!);
    }
}
