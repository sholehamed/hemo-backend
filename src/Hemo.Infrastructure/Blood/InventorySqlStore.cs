using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Blood;
using Hemo.Application.Cqrs;
using Hemo.Domain.Blood;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Blood;

/// <summary>No inferred compatibility: a documented laboratory result and independent review are required.</summary>
public sealed class InventorySqlStore(HemoDbContext db,HemoAuditWriter audit) : IInventoryStore
{
    private static InventoryUnitView View(BloodUnit x) => new(x.BloodUnitId,x.UnitCode,
        x.ProductTypeId,x.BloodGroupId,x.CurrentFacilityId,x.ExpiryDate,x.Status,
        x.IsQuarantined,Convert.ToBase64String(x.RowVersion));
    private static CrossmatchView View(Crossmatch x) => new(x.CrossmatchId,x.BloodUnitId,
        x.BloodRequestItemId,x.Status,x.CompatibilityResult,x.PerformedByUserId,
        x.ApprovedByUserId,Convert.ToBase64String(x.RowVersion));

    public async Task<IReadOnlyList<InventoryUnitView>> ListAsync(long facility,CancellationToken ct) =>
        (await db.BloodUnits.AsNoTracking().Where(x=>x.CurrentFacilityId==facility)
            .OrderBy(x=>x.ExpiryDate).Take(300).ToArrayAsync(ct))
            .Select(View).ToArray();


    public async Task<IReadOnlyList<BankWorkItem>> WorkAsync(long facility,CancellationToken ct)
    {
        var rows=await (from res in db.UnitReservations
            join unit in db.BloodUnits on res.BloodUnitId equals unit.BloodUnitId
            join item in db.BloodRequestItems on res.BloodRequestItemId equals item.BloodRequestItemId
            join req in db.BloodRequests on item.BloodRequestId equals req.BloodRequestId
            join cross in db.Crossmatches
                on new { res.BloodUnitId,res.BloodRequestItemId }
                equals new { cross.BloodUnitId,cross.BloodRequestItemId } into matches
            from x in matches.DefaultIfEmpty()
            where unit.CurrentFacilityId==facility && res.Status=="Active"
            orderby res.ReservedAt descending
            select new {res.UnitReservationId,RequestId=req.BloodRequestId,
                UnitId=unit.BloodUnitId,unit.UnitCode,ReservationStatus=res.Status,
                CrossmatchId=(long?)x.CrossmatchId,
                CrossmatchStatus=x.Status,CompatibilityResult=x.CompatibilityResult,
                CrossmatchVersion=x.RowVersion}).Take(300).AsNoTracking().ToArrayAsync(ct);
        return rows.Select(x=>new BankWorkItem(x.UnitReservationId,x.RequestId,x.UnitId,
            x.UnitCode,x.ReservationStatus,x.CrossmatchId,x.CrossmatchStatus,
            x.CompatibilityResult,x.CrossmatchVersion==null?null:
                Convert.ToBase64String(x.CrossmatchVersion))).ToArray();
    }

    public async Task<IReadOnlyList<BloodGroupOption>> GroupsAsync(CancellationToken ct) =>
        await db.BloodGroups.AsNoTracking().OrderBy(x=>x.Code)
            .Select(x=>new BloodGroupOption(x.BloodGroupId,x.Code)).ToArrayAsync(ct);

    public async Task<Outcome<InventoryUnitView>> ReceiveAsync(long actor,long facility,
        RegisterUnitRequest input,CancellationToken ct)
    {
        var code=input.UnitCode?.Trim()??"";
        if(code.Length is < 3 or > 80 || input.ProductTypeId<=0 || input.BloodGroupId<=0 ||
           input.ExpiryDate<=DateOnly.FromDateTime(DateTime.UtcNow) ||
           (input.StorageLocation?.Length??0)>120)
            return Outcome<InventoryUnitView>.Fail("invalid_unit");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        if(await db.BloodUnits.AnyAsync(x=>x.UnitCode==code,ct))
            return Outcome<InventoryUnitView>.Fail("duplicate_unit");
        if(!await db.BloodProductTypes.AnyAsync(x=>x.ProductTypeId==input.ProductTypeId&&x.IsActive,ct) ||
           !await db.BloodGroups.AnyAsync(x=>x.BloodGroupId==input.BloodGroupId,ct))
            return Outcome<InventoryUnitView>.Fail("invalid_catalog");
        var unit=new BloodUnit{UnitCode=code,ProductTypeId=input.ProductTypeId,
            BloodGroupId=input.BloodGroupId,CurrentFacilityId=facility,
            ExpiryDate=input.ExpiryDate,ReceivedAt=DateTime.UtcNow,
            Status="Available",StorageLocation=input.StorageLocation?.Trim()};
        db.BloodUnits.Add(unit);
        try
        {
            await db.SaveChangesAsync(ct);
            db.InventoryMovements.Add(new InventoryMovement{BloodUnitId=unit.BloodUnitId,
                ToFacilityId=facility,MovementType="ManualReceipt",PerformedByUserId=actor,
                PriorStatus="PendingReceipt",NewStatus="Available",ReferenceType="BloodUnit",
                ReferenceId=unit.BloodUnitId});
            audit.Record(actor,facility,"BloodUnit",unit.BloodUnitId,"Received");
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Outcome<InventoryUnitView>.Ok(View(unit));
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<InventoryUnitView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<long>> ReserveAsync(long actor,long facility,
        ReserveUnitRequest input,CancellationToken ct)
    {
        if(input.UnitId<=0||input.RequestId<=0)return Outcome<long>.Fail("invalid_request");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var request=await (from r in db.BloodRequests
            join e in db.Encounters on r.EncounterId equals e.EncounterId
            join l in db.EncounterLocations on e.EncounterId equals l.EncounterId
            join item in db.BloodRequestItems on r.BloodRequestId equals item.BloodRequestId
            where r.BloodRequestId==input.RequestId && e.FacilityId==facility &&
                l.ToAt==null && r.Status=="Approved"
            select new{Request=r,Item=item}).SingleOrDefaultAsync(ct);
        if(request is null)return Outcome<long>.Fail("request_not_approved");
        var unit=await db.BloodUnits.SingleOrDefaultAsync(x=>x.BloodUnitId==input.UnitId
            && x.CurrentFacilityId==facility,ct);
        if(unit is null)return Outcome<long>.Fail("unit_not_found");
        if(unit.Status!="Available"||unit.IsQuarantined||
            unit.ExpiryDate<=DateOnly.FromDateTime(DateTime.UtcNow))
            return Outcome<long>.Fail("unit_unavailable");
        if(unit.ProductTypeId!=request.Item.ProductTypeId)
            return Outcome<long>.Fail("product_mismatch");
        var reservedCount=await db.UnitReservations.CountAsync(x=>
            x.BloodRequestItemId==request.Item.BloodRequestItemId &&
                (x.Status=="Active"||x.Status=="Consumed"),ct);
        if(reservedCount>=request.Item.RequestedQuantity)
            return Outcome<long>.Fail("quantity_exceeded");
        if(await db.UnitReservations.AnyAsync(x=>x.BloodUnitId==input.UnitId&&x.Status=="Active",ct))
            return Outcome<long>.Fail("already_reserved");
        var reservation=new UnitReservation{BloodUnitId=unit.BloodUnitId,
            BloodRequestItemId=request.Item.BloodRequestItemId,ReservedByUserId=actor,
            ReservedAt=DateTime.UtcNow,Status="Active"};
        db.UnitReservations.Add(reservation);unit.Status="Reserved";
        try
        {
            await db.SaveChangesAsync(ct);
            db.InventoryMovements.Add(new InventoryMovement{BloodUnitId=unit.BloodUnitId,
                FromFacilityId=facility,ToFacilityId=facility,MovementType="Reservation",
                PerformedByUserId=actor,ReferenceType="UnitReservation",
                ReferenceId=reservation.UnitReservationId,
                PriorStatus="Available",NewStatus="Reserved"});
            audit.Record(actor,facility,"UnitReservation",reservation.UnitReservationId,"Reserved");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<long>.Ok(reservation.UnitReservationId);
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<long>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<CrossmatchView>> RecordAsync(long actor,long facility,
        RecordCrossmatchRequest input,CancellationToken ct)
    {
        if(input.ReservationId<=0 || input.CompatibilityResult is not
             ("Compatible" or "Incompatible" or "Discrepancy" or "NotApplicable") ||
           input.TestCode is null || input.TestCode.Trim().Length is < 2 or > 60 ||
           input.TestResult is null || input.TestResult.Trim().Length is < 1 or > 35 ||
           (input.Comments?.Length??0)>1500)
            return Outcome<CrossmatchView>.Fail("invalid_crossmatch");

        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from res in db.UnitReservations
            join unit in db.BloodUnits on res.BloodUnitId equals unit.BloodUnitId
            join item in db.BloodRequestItems on res.BloodRequestItemId equals item.BloodRequestItemId
            join request in db.BloodRequests on item.BloodRequestId equals request.BloodRequestId
            join encounter in db.Encounters on request.EncounterId equals encounter.EncounterId
            where res.UnitReservationId==input.ReservationId && res.Status=="Active" &&
                unit.CurrentFacilityId==facility && encounter.FacilityId==facility &&
                request.Status=="Approved"
            select new{Reservation=res,Unit=unit}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<CrossmatchView>.Fail("reservation_not_found");
        if(row.Unit.IsQuarantined||row.Unit.ExpiryDate<=DateOnly.FromDateTime(DateTime.UtcNow)
            ||row.Unit.Status!="Reserved")
            return Outcome<CrossmatchView>.Fail("unit_unavailable");
        if(await db.Crossmatches.AnyAsync(x=>x.BloodUnitId==row.Unit.BloodUnitId
            && x.BloodRequestItemId==row.Reservation.BloodRequestItemId,ct))
            return Outcome<CrossmatchView>.Fail("crossmatch_exists");

        var check=new Crossmatch{BloodUnitId=row.Unit.BloodUnitId,
            BloodRequestItemId=row.Reservation.BloodRequestItemId,PerformedByUserId=actor,
            PerformedAt=DateTime.UtcNow,CompatibilityResult=input.CompatibilityResult,
            Comments=input.Comments?.Trim(),Status="PendingReview",
            UnitLabelBloodGroupId=row.Unit.BloodGroupId};
        db.Crossmatches.Add(check);row.Unit.Status="Crossmatch";
        try
        {
            await db.SaveChangesAsync(ct);
            db.CrossmatchTests.Add(new CrossmatchTest{CrossmatchId=check.CrossmatchId,
                TestCode=input.TestCode.Trim(),Result=input.TestResult.Trim(),
                ObservedAt=DateTime.UtcNow});
            audit.Record(actor,facility,"Crossmatch",check.CrossmatchId,"Recorded");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<CrossmatchView>.Ok(View(check));
        }
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<CrossmatchView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<CrossmatchView>> FinalizeAsync(long actor,long facility,
        long crossmatchId,FinalizeCrossmatchRequest input,CancellationToken ct)
    {
        byte[] version;
        try{version=Convert.FromBase64String(input.RowVersion??"");}
        catch(FormatException){return Outcome<CrossmatchView>.Fail("invalid_rowversion");}
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var row=await (from check in db.Crossmatches
            join unit in db.BloodUnits on check.BloodUnitId equals unit.BloodUnitId
            join res in db.UnitReservations on new{check.BloodUnitId,check.BloodRequestItemId}
                equals new{res.BloodUnitId,res.BloodRequestItemId}
            where check.CrossmatchId==crossmatchId && unit.CurrentFacilityId==facility
                && res.Status=="Active"
            select new{Check=check,Unit=unit}).SingleOrDefaultAsync(ct);
        if(row is null)return Outcome<CrossmatchView>.Fail("not_found");
        if(row.Check.PerformedByUserId==actor)
            return Outcome<CrossmatchView>.Fail("independent_review_required");
        if(row.Check.Status!="PendingReview"||row.Unit.Status!="Crossmatch"||
           row.Unit.IsQuarantined)
            return Outcome<CrossmatchView>.Fail("invalid_state");
        if(version.Length!=row.Check.RowVersion.Length||
           !CryptographicOperations.FixedTimeEquals(version,row.Check.RowVersion))
            return Outcome<CrossmatchView>.Fail("concurrency_conflict");
        if(!await db.CrossmatchTests.AnyAsync(x=>x.CrossmatchId==crossmatchId,ct))
            return Outcome<CrossmatchView>.Fail("missing_test_evidence");
        row.Check.Status="Final";row.Check.ApprovedByUserId=actor;
        row.Check.ApprovedAt=DateTime.UtcNow;
        audit.Record(actor,facility,"Crossmatch",crossmatchId,"Finalized");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<CrossmatchView>.Ok(View(row.Check));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<CrossmatchView>.Fail("concurrency_conflict");}
    }
}
