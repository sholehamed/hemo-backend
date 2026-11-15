using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Blood;
using Hemo.Application.Cqrs;
using Hemo.Domain.Blood;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Blood;

/// <summary>
/// SQL atomic handoffs. An issue is never created without an approved request,
/// active reservation, and independently finalized compatible crossmatch.
/// </summary>
public sealed class LogisticsSqlStore(HemoDbContext db,HemoAuditWriter audit):ILogisticsStore
{
    private static IssueView View(UnitIssue issue,string unitCode,long requestId) =>
        new(issue.UnitIssueId,issue.BloodUnitId,unitCode,requestId,
            issue.IssuedToDepartmentId,issue.Status,issue.IssuedAt,
            Convert.ToBase64String(issue.RowVersion));
    private static ReturnView View(UnitReturn ret) => new(ret.UnitReturnId,
        ret.UnitIssueId,ret.Status,ret.Decision,Convert.ToBase64String(ret.RowVersion));

    public async Task<IReadOnlyList<ReturnView>> ReturnsAsync(long facility,CancellationToken ct)
    {
        var records=await (from ret in db.UnitReturns
            join unit in db.BloodUnits on ret.BloodUnitId equals unit.BloodUnitId
            where unit.CurrentFacilityId==facility
            orderby ret.ReturnedAt descending
            select ret).AsNoTracking().Take(200).ToArrayAsync(ct);
        return records.Select(View).ToArray();
    }

    public async Task<IReadOnlyList<IssueView>> ListAsync(
        long facility,long department,CancellationToken ct)
    {
        var result=await (from issue in db.UnitIssues
            join unit in db.BloodUnits on issue.BloodUnitId equals unit.BloodUnitId
            join item in db.BloodRequestItems on issue.BloodRequestItemId equals item.BloodRequestItemId
            join request in db.BloodRequests on item.BloodRequestId equals request.BloodRequestId
            where unit.CurrentFacilityId==facility && issue.IssuedToDepartmentId==department
            orderby issue.IssuedAt descending
            select new {Issue=issue,unit.UnitCode,RequestId=request.BloodRequestId})
            .AsNoTracking().Take(200).ToArrayAsync(ct);
        return result.Select(x=>View(x.Issue,x.UnitCode,x.RequestId)).ToArray();
    }

    public async Task<Outcome<IssueView>> IssueAsync(long actor,long facility,
        IssueUnitRequest input,CancellationToken ct)
    {
        if(input.UnitId<=0||input.RequestId<=0||input.DepartmentId<=0||
           (input.ConditionNote?.Length??0)>1000)
            return Outcome<IssueView>.Fail("invalid_issue");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from item in db.BloodRequestItems
            join request in db.BloodRequests on item.BloodRequestId equals request.BloodRequestId
            join encounter in db.Encounters on request.EncounterId equals encounter.EncounterId
            join loc in db.EncounterLocations on encounter.EncounterId equals loc.EncounterId
            where request.BloodRequestId==input.RequestId && request.Status=="Approved"
                && encounter.FacilityId==facility && encounter.Status=="Admitted"
                && loc.DepartmentId==input.DepartmentId && loc.ToAt==null
            select new{Item=item,Request=request}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<IssueView>.Fail("request_not_approved");
        var unit=await db.BloodUnits.SingleOrDefaultAsync(x=>x.BloodUnitId==input.UnitId
            && x.CurrentFacilityId==facility,ct);
        if(unit is null)return Outcome<IssueView>.Fail("unit_not_found");
        if(unit.IsQuarantined||unit.ExpiryDate<=DateOnly.FromDateTime(DateTime.UtcNow)||
            unit.ProductTypeId!=row.Item.ProductTypeId||unit.Status!="Crossmatch")
            return Outcome<IssueView>.Fail("unit_unavailable");
        var res=await db.UnitReservations.SingleOrDefaultAsync(x=>
            x.BloodUnitId==unit.BloodUnitId && x.BloodRequestItemId==row.Item.BloodRequestItemId
            && x.Status=="Active",ct);
        if(res is null)return Outcome<IssueView>.Fail("missing_reservation");
        var final=await db.Crossmatches.AnyAsync(x=>x.BloodUnitId==unit.BloodUnitId
            && x.BloodRequestItemId==row.Item.BloodRequestItemId
            && x.Status=="Final" && x.CompatibilityResult=="Compatible"
            && x.ApprovedByUserId!=null &&
            x.ApprovedByUserId!=x.PerformedByUserId
            && db.CrossmatchTests.Any(t=>t.CrossmatchId==x.CrossmatchId),ct);
        if(!final)return Outcome<IssueView>.Fail("crossmatch_not_approved");
        if(await db.UnitIssues.AnyAsync(x=>x.BloodUnitId==unit.BloodUnitId
            && x.Status!="Cancelled",ct))
            return Outcome<IssueView>.Fail("already_issued");
        var now=DateTime.UtcNow;
        var issue=new UnitIssue{BloodUnitId=unit.BloodUnitId,
            BloodRequestItemId=row.Item.BloodRequestItemId,
            IssuedToDepartmentId=input.DepartmentId,IssuedByUserId=actor,
            IssueNo="ISS-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),
            IssuedAt=now,Status="Issued",TransportConditionNote=input.ConditionNote?.Trim()};
        db.UnitIssues.Add(issue);
        unit.Status="Issued";res.Status="Consumed";
        try
        {
            await db.SaveChangesAsync(ct);
            db.InventoryMovements.Add(new InventoryMovement{BloodUnitId=unit.BloodUnitId,
                FromFacilityId=facility,ToFacilityId=facility,MovementType="Issue",
                PerformedByUserId=actor,ReferenceType="UnitIssue",ReferenceId=issue.UnitIssueId,
                PriorStatus="Crossmatch",NewStatus="Issued"});
            audit.Record(actor,facility,"UnitIssue",issue.UnitIssueId,"Issued");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<IssueView>.Ok(View(issue,unit.UnitCode,input.RequestId));
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<IssueView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<DispatchView>> DispatchAsync(long actor,long facility,
        CreateDispatchRequest input,CancellationToken ct)
    {
        if(input.IssueId<=0||string.IsNullOrWhiteSpace(input.ContainerType)||
            input.ContainerType.Trim().Length>80||input.TemperatureAtExitC is < -50 or > 60||
            (input.Note?.Length??0)>1200)
            return Outcome<DispatchView>.Fail("invalid_dispatch");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from issue in db.UnitIssues
            join unit in db.BloodUnits on issue.BloodUnitId equals unit.BloodUnitId
            where issue.UnitIssueId==input.IssueId && unit.CurrentFacilityId==facility
            select new{Issue=issue,Unit=unit}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<DispatchView>.Fail("not_found");
        if(row.Issue.Status!="Issued"||row.Unit.Status!="Issued")
            return Outcome<DispatchView>.Fail("invalid_state");
        if(await db.DispatchItems.AnyAsync(x=>x.UnitIssueId==input.IssueId,ct))
            return Outcome<DispatchView>.Fail("already_dispatched");
        var now=DateTime.UtcNow;
        var dispatch=new Dispatch{FacilityId=facility,DepartmentId=row.Issue.IssuedToDepartmentId,
            DispatchNo="DSP-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),
            CreatedByUserId=actor,CreatedAtDispatch=now,SentAt=now,Status="InTransit",
            ContainerType=input.ContainerType.Trim(),TemperatureAtExitC=input.TemperatureAtExitC,
            Note=input.Note?.Trim()};
        db.Dispatches.Add(dispatch);
        row.Issue.Status="InTransit";row.Unit.Status="InTransit";
        try
        {
            await db.SaveChangesAsync(ct);
            db.DispatchItems.Add(new DispatchItem{DispatchId=dispatch.DispatchId,
                UnitIssueId=row.Issue.UnitIssueId});
            db.InventoryMovements.Add(new InventoryMovement{BloodUnitId=row.Unit.BloodUnitId,
                FromFacilityId=facility,ToFacilityId=facility,MovementType="Dispatch",
                PerformedByUserId=actor,ReferenceType="Dispatch",ReferenceId=dispatch.DispatchId,
                PriorStatus="Issued",NewStatus="InTransit"});
            audit.Record(actor,facility,"Dispatch",dispatch.DispatchId,"Sent");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<DispatchView>.Ok(new DispatchView(dispatch.DispatchId,
                input.IssueId,dispatch.Status,dispatch.DispatchNo));
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<DispatchView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<IssueView>> ReceiveAsync(long actor,long facility,long department,
        long issueId,ReceiveIssueRequest input,CancellationToken ct)
    {
        if(issueId<=0||(input.ConditionNote?.Length??0)>1000)
            return Outcome<IssueView>.Fail("invalid_receipt");
        byte[] expected;
        try{expected=Convert.FromBase64String(input.RowVersion??"");}
        catch(FormatException){return Outcome<IssueView>.Fail("invalid_rowversion");}
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from issue in db.UnitIssues
            join unit in db.BloodUnits on issue.BloodUnitId equals unit.BloodUnitId
            join item in db.BloodRequestItems on issue.BloodRequestItemId equals item.BloodRequestItemId
            join request in db.BloodRequests on item.BloodRequestId equals request.BloodRequestId
            where issue.UnitIssueId==issueId && unit.CurrentFacilityId==facility
                && issue.IssuedToDepartmentId==department
            select new{Issue=issue,Unit=unit,RequestId=request.BloodRequestId})
            .SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<IssueView>.Fail("not_found");
        if(row.Issue.Status is not ("Issued" or "InTransit")||row.Issue.IssuedByUserId==actor)
            return Outcome<IssueView>.Fail("invalid_state");
        if(expected.Length!=row.Issue.RowVersion.Length||
            !CryptographicOperations.FixedTimeEquals(expected,row.Issue.RowVersion))
            return Outcome<IssueView>.Fail("concurrency_conflict");
        row.Issue.Status="Received";row.Issue.ReceivedByUserId=actor;
        row.Issue.ReceivedAt=DateTime.UtcNow;
        if(input.ConditionNote is {Length:>0})row.Issue.TransportConditionNote=input.ConditionNote.Trim();
        row.Unit.Status="Issued";
        var dispatch=await (from di in db.DispatchItems
            join d in db.Dispatches on di.DispatchId equals d.DispatchId
            where di.UnitIssueId==issueId && d.FacilityId==facility
            select d).SingleOrDefaultAsync(ct);
        if(dispatch is not null){dispatch.Status="Delivered";
            dispatch.DeliveredAt=row.Issue.ReceivedAt;dispatch.ReceiverUserId=actor;}
        audit.Record(actor,facility,"UnitIssue",issueId,"Received");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<IssueView>.Ok(View(row.Issue,row.Unit.UnitCode,row.RequestId));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<IssueView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<ReturnView>> ReturnAsync(long actor,long facility,long department,
        long issueId,ReturnIssueRequest input,CancellationToken ct)
    {
        if(issueId<=0 || input.ReturnedByUserId<=0 ||
            input.ReasonCode is null || input.ReasonCode.Trim().Length is < 2 or > 60 ||
            (input.ConditionCode?.Length??0)>40 ||(input.Notes?.Length??0)>1400||
            input.TemperatureC is < -50 or > 60)
            return Outcome<ReturnView>.Fail("invalid_return");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from issue in db.UnitIssues
            join unit in db.BloodUnits on issue.BloodUnitId equals unit.BloodUnitId
            where issue.UnitIssueId==issueId && issue.IssuedToDepartmentId==department
                && unit.CurrentFacilityId==facility
            select new{Issue=issue,Unit=unit}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<ReturnView>.Fail("not_found");
        if(row.Issue.Status!="Received" || row.Issue.ReceivedByUserId!=input.ReturnedByUserId)
            return Outcome<ReturnView>.Fail("invalid_state");
        if(await db.UnitReturns.AnyAsync(x=>x.UnitIssueId==issueId,ct))
            return Outcome<ReturnView>.Fail("already_returned");
        // Never return into available stock automatically. Full disposal/restock SOP is external.
        row.Issue.Status="Returned";row.Unit.Status="Quarantined";row.Unit.IsQuarantined=true;
        var ret=new UnitReturn{UnitIssueId=issueId,BloodUnitId=row.Unit.BloodUnitId,
            ReturnedByUserId=input.ReturnedByUserId,ReceivedByUserId=actor,
            ReturnedAt=DateTime.UtcNow,ReasonCode=input.ReasonCode.Trim(),
            ConditionCode=input.ConditionCode?.Trim(),TemperatureC=input.TemperatureC,
            Notes=input.Notes?.Trim(),Status="Pending"};
        db.UnitReturns.Add(ret);
        try
        {
            await db.SaveChangesAsync(ct);
            db.InventoryMovements.Add(new InventoryMovement{BloodUnitId=row.Unit.BloodUnitId,
                FromFacilityId=facility,ToFacilityId=facility,MovementType="ReturnQuarantine",
                PerformedByUserId=actor,ReferenceType="UnitReturn",ReferenceId=ret.UnitReturnId,
                PriorStatus="Issued",NewStatus="Quarantined"});
            audit.Record(actor,facility,"UnitReturn",ret.UnitReturnId,"Returned");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReturnView>.Ok(View(ret));
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<ReturnView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<ReturnView>> ReviewReturnAsync(long actor,long facility,
        long returnId,ReviewReturnRequest input,CancellationToken ct)
    {
        if(input.Decision is not ("Quarantine" or "Waste")||
            (input.Note?.Length??0)>1400)
            return Outcome<ReturnView>.Fail("invalid_review");
        byte[] expected;
        try{expected=Convert.FromBase64String(input.RowVersion??"");}
        catch(FormatException){return Outcome<ReturnView>.Fail("invalid_rowversion");}
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from ret in db.UnitReturns
            join unit in db.BloodUnits on ret.BloodUnitId equals unit.BloodUnitId
            where ret.UnitReturnId==returnId && unit.CurrentFacilityId==facility
            select new{Return=ret,Unit=unit}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<ReturnView>.Fail("not_found");
        if(row.Return.Status!="Pending"||row.Return.ReceivedByUserId==actor)
            return Outcome<ReturnView>.Fail("independent_review_required");
        if(expected.Length!=row.Return.RowVersion.Length||
            !CryptographicOperations.FixedTimeEquals(expected,row.Return.RowVersion))
            return Outcome<ReturnView>.Fail("concurrency_conflict");
        row.Return.Status="Approved";row.Return.Decision=input.Decision;
        row.Return.ReviewedByUserId=actor;row.Return.ReviewedAt=DateTime.UtcNow;
        row.Return.Notes=input.Note?.Trim()??row.Return.Notes;
        if(input.Decision=="Waste")
        {
            row.Unit.Status="Wasted";
            db.UnitWastes.Add(new UnitWaste{BloodUnitId=row.Unit.BloodUnitId,
                UnitReturnId=returnId,WasteCode="WST-"+Guid.NewGuid().ToString("N"),
                ReasonCode="ReturnReview",WasteDate=DateTime.UtcNow,
                RecordedByUserId=row.Return.ReceivedByUserId,ApprovedByUserId=actor,
                ApprovedAt=DateTime.UtcNow});
        }
        audit.Record(actor,facility,"UnitReturn",returnId,input.Decision);
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReturnView>.Ok(View(row.Return));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<ReturnView>.Fail("concurrency_conflict");}
    }
}
