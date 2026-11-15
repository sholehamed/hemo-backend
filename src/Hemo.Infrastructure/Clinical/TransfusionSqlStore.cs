using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Clinical;
using Hemo.Application.Cqrs;
using Hemo.Domain.Clinical;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Clinical;

/// <summary>
/// Persistent, append-only clinical observations. No automatic compatibility or
/// physiological threshold/diagnosis inference is performed.
/// </summary>
public sealed class TransfusionSqlStore(HemoDbContext db,HemoAuditWriter audit)
    : ITransfusionStore
{
    private IQueryable<Transfusion> Scoped(long facility,long department) =>
        from t in db.Transfusions
        join e in db.Encounters on t.EncounterId equals e.EncounterId
        join l in db.EncounterLocations on t.EncounterId equals l.EncounterId
        where e.FacilityId==facility && l.DepartmentId==department && l.ToAt==null
        select t;

    private async Task<TransfusionView> ViewAsync(Transfusion t,CancellationToken ct)
    {
        var checklist=await db.TransfusionChecklists.AsNoTracking().AnyAsync(x=>
            x.TransfusionId==t.TransfusionId && x.PatientIdentityMatched &&
            x.UnitIdentityMatched && x.BloodGroupChecked && x.CompatibilityChecked &&
            x.ExpiryChecked && x.BaselineVitalChecked,ct);
        var baseline=await db.VitalObservations.AsNoTracking().AnyAsync(x=>
            x.TransfusionId==t.TransfusionId && x.TimepointCode=="Baseline",ct);
        var signed=await db.TransfusionSignoffs.AsNoTracking().AnyAsync(x=>
            x.TransfusionId==t.TransfusionId,ct);
        var issueId=await db.UnitIssues.AsNoTracking()
            .Where(x=>x.BloodUnitId==t.BloodUnitId &&
                x.BloodRequestItemId==t.BloodRequestItemId && x.Status!="Cancelled")
            .Select(x=>(long?)x.UnitIssueId).FirstOrDefaultAsync(ct);
        return new TransfusionView(t.TransfusionId,t.EncounterId,t.BloodRequestItemId,
            t.BloodUnitId,issueId,t.Status,t.StartedAt,t.EndedAt,t.StopReason,
            Convert.ToBase64String(t.RowVersion),checklist,baseline,signed);
    }
    private static VitalView View(VitalObservation v)=>new(v.VitalObservationId,v.TransfusionId,
        v.ObservedAt,v.TimepointCode,v.SystolicBp,v.DiastolicBp,v.HeartRate,v.TemperatureC,
        v.Spo2,v.RespiratoryRate,v.AlertFlag,v.ClinicalNote);
    private static bool Version(byte[] current,string? encoded)
    {
        try {
            var incoming=Convert.FromBase64String(encoded??"");
            return incoming.Length==current.Length &&
                CryptographicOperations.FixedTimeEquals(incoming,current);
        } catch(FormatException) { return false; }
    }

    public async Task<IReadOnlyList<TransfusionView>> ListAsync(
        long facility,long department,CancellationToken ct)
    {
        var rows=await Scoped(facility,department).AsNoTracking()
            .OrderByDescending(x=>x.CreatedAt).ThenByDescending(x=>x.TransfusionId)
            .Take(100).ToArrayAsync(ct);
        var result=new List<TransfusionView>();
        foreach(var row in rows)result.Add(await ViewAsync(row,ct));
        return result;
    }

    public async Task<Outcome<TransfusionDetails>> GetAsync(
        long facility,long department,long id,CancellationToken ct)
    {
        var t=await Scoped(facility,department).AsNoTracking()
            .SingleOrDefaultAsync(x=>x.TransfusionId==id,ct);
        if(t is null)return Outcome<TransfusionDetails>.Fail("not_found");
        var vital=await db.VitalObservations.AsNoTracking()
            .Where(x=>x.TransfusionId==id).OrderBy(x=>x.ObservedAt)
            .Select(x=>x).ToArrayAsync(ct);
        var actions=await db.TransfusionActions.AsNoTracking()
            .Where(x=>x.TransfusionId==id).OrderBy(x=>x.ActionAt)
            .Select(x=>x.ActionType).ToArrayAsync(ct);
        return Outcome<TransfusionDetails>.Ok(new TransfusionDetails(
            await ViewAsync(t,ct),vital.Select(View).ToArray(),actions));
    }

    public async Task<Outcome<TransfusionView>> PrepareAsync(long actor,long facility,
        long department,PrepareTransfusionRequest input,CancellationToken ct)
    {
        if(input.IssueId<=0)return Outcome<TransfusionView>.Fail("invalid_input");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var issue=await (from i in db.UnitIssues
            join item in db.BloodRequestItems on i.BloodRequestItemId equals item.BloodRequestItemId
            join request in db.BloodRequests on item.BloodRequestId equals request.BloodRequestId
            join encounter in db.Encounters on request.EncounterId equals encounter.EncounterId
            join loc in db.EncounterLocations on encounter.EncounterId equals loc.EncounterId
            join unit in db.BloodUnits on i.BloodUnitId equals unit.BloodUnitId
            where i.UnitIssueId==input.IssueId && i.IssuedToDepartmentId==department
                && i.Status=="Received" && i.ReceivedByUserId!=null
                && encounter.Status=="Admitted" && encounter.FacilityId==facility
                && loc.DepartmentId==department && loc.ToAt==null
                && request.Status=="Approved" && unit.CurrentFacilityId==facility
                && unit.Status=="Issued" && !unit.IsQuarantined
                && unit.ExpiryDate>DateOnly.FromDateTime(DateTime.UtcNow)
            select new {Issue=i,Encounter=encounter,Item=item,Unit=unit})
            .SingleOrDefaultAsync(ct);
        if(issue is null)return Outcome<TransfusionView>.Fail("unit_not_received");
        if(await db.Transfusions.AnyAsync(t=>t.BloodUnitId==issue.Unit.BloodUnitId,ct))
            return Outcome<TransfusionView>.Fail("transfusion_exists");
        var latestIdentity=await db.IdentityVerifications.AsNoTracking()
            .Where(x=>x.EncounterId==issue.Encounter.EncounterId)
            .OrderByDescending(x=>x.VerifiedAt)
            .ThenByDescending(x=>x.IdentityVerificationId)
            .Select(x=>x.Status).FirstOrDefaultAsync(ct);
        if(latestIdentity!="Verified")
            return Outcome<TransfusionView>.Fail("identity_not_verified");
        var approvedCrossmatch=await db.Crossmatches.AsNoTracking().AnyAsync(x=>
            x.BloodUnitId==issue.Unit.BloodUnitId &&
            x.BloodRequestItemId==issue.Item.BloodRequestItemId &&
            x.Status=="Final" && x.CompatibilityResult=="Compatible" &&
            x.ApprovedByUserId!=null && x.ApprovedByUserId!=x.PerformedByUserId &&
            db.CrossmatchTests.Any(test=>test.CrossmatchId==x.CrossmatchId),ct);
        if(!approvedCrossmatch)return Outcome<TransfusionView>.Fail("crossmatch_not_final");
        var t=new Transfusion{EncounterId=issue.Encounter.EncounterId,
            BloodRequestItemId=issue.Item.BloodRequestItemId,
            BloodUnitId=issue.Unit.BloodUnitId,Status="Planned"};
        db.Transfusions.Add(t);
        try {
            await db.SaveChangesAsync(ct);
            db.TransfusionActions.Add(new TransfusionAction{TransfusionId=t.TransfusionId,
                ActionType="Prepared",PerformedByUserId=actor,ActionAt=DateTime.UtcNow});
            audit.Record(actor,facility,"Transfusion",t.TransfusionId,"Prepared");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<TransfusionView>.Ok(await ViewAsync(t,ct));
        } catch(DbUpdateException) {
            await tx.RollbackAsync(ct);
            return Outcome<TransfusionView>.Fail("constraint_conflict");
        }
    }

    public async Task<Outcome<TransfusionView>> ChecklistAsync(long actor,long facility,
        long department,long id,ChecklistRequest input,CancellationToken ct)
    {
        if(!input.PatientIdentityMatched||!input.UnitIdentityMatched||
           !input.BloodGroupChecked||!input.CompatibilityChecked||!input.ExpiryChecked||
           !input.BaselineVitalChecked||(input.Note?.Length??0)>800)
            return Outcome<TransfusionView>.Fail("checklist_incomplete");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var t=await Scoped(facility,department).SingleOrDefaultAsync(x=>x.TransfusionId==id,ct);
        if(t is null)return Outcome<TransfusionView>.Fail("not_found");
        if(t.Status!="Planned" || await db.TransfusionChecklists.AnyAsync(x=>x.TransfusionId==id,ct))
            return Outcome<TransfusionView>.Fail("invalid_state");
        if(!await db.VitalObservations.AnyAsync(x=>x.TransfusionId==id && x.TimepointCode=="Baseline",ct))
            return Outcome<TransfusionView>.Fail("baseline_required");
        var latestIdentity=await db.IdentityVerifications.AsNoTracking()
            .Where(x=>x.EncounterId==t.EncounterId)
            .OrderByDescending(x=>x.VerifiedAt).ThenByDescending(x=>x.IdentityVerificationId)
            .Select(x=>x.Status).FirstOrDefaultAsync(ct);
        if(latestIdentity!="Verified")return Outcome<TransfusionView>.Fail("identity_not_verified");
        db.TransfusionChecklists.Add(new TransfusionChecklist{
            TransfusionId=id,PatientIdentityMatched=true,UnitIdentityMatched=true,
            BloodGroupChecked=true,CompatibilityChecked=true,ExpiryChecked=true,
            BaselineVitalChecked=true,CheckedByUserId=actor,CheckedAt=DateTime.UtcNow,
            Note=input.Note?.Trim()});
        audit.Record(actor,facility,"Transfusion",id,"ChecklistVerified");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return Outcome<TransfusionView>.Ok(await ViewAsync(t,ct));
    }

    public async Task<Outcome<VitalView>> VitalAsync(long actor,long facility,long department,
        long id,VitalRequest input,CancellationToken ct)
    {
        if(input.TimepointCode is not ("Baseline" or "During" or "End" or "FollowUp") ||
           input.ObservedAt.Kind!=DateTimeKind.Utc ||
           input.ObservedAt>DateTime.UtcNow.AddMinutes(5) ||
           input.ObservedAt<DateTime.UtcNow.AddDays(-7) ||
           (input.ClinicalNote?.Length??0)>1200 ||
           input.TemperatureC is <20 or >45 ||input.Spo2 is >100 ||
           input.HeartRate is <0 or >300 ||input.RespiratoryRate is <0 or >100 ||
           input.SystolicBp is <0 or >350 ||input.DiastolicBp is <0 or >250 ||
           input.UrineOutputMl is <0 or >100000 ||
           (input.SystolicBp is null && input.DiastolicBp is null &&
           input.HeartRate is null && input.TemperatureC is null &&
           input.Spo2 is null && input.RespiratoryRate is null))
            return Outcome<VitalView>.Fail("invalid_vitals");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var t=await Scoped(facility,department).SingleOrDefaultAsync(x=>x.TransfusionId==id,ct);
        if(t is null)return Outcome<VitalView>.Fail("not_found");
        if(await db.TransfusionSignoffs.AnyAsync(x=>x.TransfusionId==id,ct))
            return Outcome<VitalView>.Fail("signed_record_locked");
        if((input.TimepointCode=="Baseline" && t.Status!="Planned") ||
           (input.TimepointCode!="Baseline" && t.Status!="InProgress") ||
           (t.StartedAt!=null && input.TimepointCode!="Baseline" &&
               input.ObservedAt<t.StartedAt))
            return Outcome<VitalView>.Fail("invalid_state");
        if(input.TimepointCode=="Baseline" &&
           await db.VitalObservations.AnyAsync(x=>x.TransfusionId==id &&
                x.TimepointCode=="Baseline",ct))
            return Outcome<VitalView>.Fail("baseline_exists");
        var v=new VitalObservation{TransfusionId=id,ObservedAt=input.ObservedAt,
            TimepointCode=input.TimepointCode,SystolicBp=input.SystolicBp,
            DiastolicBp=input.DiastolicBp,HeartRate=input.HeartRate,
            TemperatureC=input.TemperatureC,Spo2=input.Spo2,
            RespiratoryRate=input.RespiratoryRate,UrineOutputMl=input.UrineOutputMl,
            RecordedByUserId=actor,AlertFlag=input.AlertFlag,
            ClinicalNote=input.ClinicalNote?.Trim()};
        db.VitalObservations.Add(v);
        await db.SaveChangesAsync(ct);
        audit.Record(actor,facility,"VitalObservation",v.VitalObservationId,"Recorded");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return Outcome<VitalView>.Ok(View(v));
    }

    public async Task<Outcome<TransfusionView>> TransitionAsync(long actor,long facility,
        long department,long id,TransitionRequest input,string action,CancellationToken ct)
    {
        if(action is not ("Start" or "Stop" or "Complete") ||
           (input.Reason?.Length??0)>1500 ||
           (action=="Stop" && string.IsNullOrWhiteSpace(input.Reason)))
            return Outcome<TransfusionView>.Fail("invalid_transition");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var t=await Scoped(facility,department).SingleOrDefaultAsync(x=>x.TransfusionId==id,ct);
        if(t is null)return Outcome<TransfusionView>.Fail("not_found");
        if(!Version(t.RowVersion,input.RowVersion))
            return Outcome<TransfusionView>.Fail("concurrency_conflict");
        if(await db.TransfusionSignoffs.AnyAsync(x=>x.TransfusionId==id,ct))
            return Outcome<TransfusionView>.Fail("signed_record_locked");
        var now=DateTime.UtcNow;
        if(action=="Start")
        {
            if(t.Status!="Planned")return Outcome<TransfusionView>.Fail("invalid_state");
            if(!await db.TransfusionChecklists.AnyAsync(x=>x.TransfusionId==id &&
                x.PatientIdentityMatched && x.UnitIdentityMatched &&
                x.BloodGroupChecked && x.CompatibilityChecked && x.ExpiryChecked &&
                x.BaselineVitalChecked,ct) ||
               !await db.VitalObservations.AnyAsync(x=>x.TransfusionId==id &&
                    x.TimepointCode=="Baseline",ct))
                return Outcome<TransfusionView>.Fail("precheck_required");
            var issue=await db.UnitIssues.AsNoTracking().AnyAsync(x=>
                x.BloodUnitId==t.BloodUnitId &&
                x.BloodRequestItemId==t.BloodRequestItemId && x.Status=="Received",ct);
            if(!issue)return Outcome<TransfusionView>.Fail("unit_not_received");
            var currentIdentity=await db.IdentityVerifications.AsNoTracking()
                .Where(x=>x.EncounterId==t.EncounterId)
                .OrderByDescending(x=>x.VerifiedAt).ThenByDescending(x=>x.IdentityVerificationId)
                .Select(x=>x.Status).FirstOrDefaultAsync(ct);
            if(currentIdentity!="Verified")return Outcome<TransfusionView>.Fail("identity_not_verified");
            t.Status="InProgress";t.StartedAt=now;t.StartedByUserId=actor;
        }
        else
        {
            if(t.Status!="InProgress")return Outcome<TransfusionView>.Fail("invalid_state");
            if(action=="Complete" && !await db.VitalObservations.AnyAsync(x=>
                x.TransfusionId==id && x.TimepointCode=="During",ct))
                return Outcome<TransfusionView>.Fail("monitoring_required");
            t.Status=action=="Stop"?"Stopped":"Completed";
            t.EndedAt=now;t.EndedByUserId=actor;
            if(action=="Stop")t.StopReason=input.Reason?.Trim();
            else t.FinalDisposition="Completed";
        }
        db.TransfusionActions.Add(new TransfusionAction{TransfusionId=id,
            ActionType=action,ActionAt=now,PerformedByUserId=actor,
            Description=input.Reason?.Trim()});
        audit.Record(actor,facility,"Transfusion",id,action);
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<TransfusionView>.Ok(await ViewAsync(t,ct));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<TransfusionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<TransfusionView>> SignoffAsync(long actor,long facility,
        long department,long id,SignoffRequest input,CancellationToken ct)
    {
        if((input.Comment?.Length??0)>800)return Outcome<TransfusionView>.Fail("invalid_signoff");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var t=await Scoped(facility,department).SingleOrDefaultAsync(x=>x.TransfusionId==id,ct);
        if(t is null)return Outcome<TransfusionView>.Fail("not_found");
        if(!Version(t.RowVersion,input.RowVersion))
            return Outcome<TransfusionView>.Fail("concurrency_conflict");
        if(t.Status is not ("Stopped" or "Completed") ||
           t.EndedByUserId==actor || await db.TransfusionSignoffs.AnyAsync(x=>x.TransfusionId==id,ct))
            return Outcome<TransfusionView>.Fail("independent_signoff_required");
        db.TransfusionSignoffs.Add(new TransfusionSignoff{TransfusionId=id,
            SignerUserId=actor,SignoffRole="Nurse",SignedAt=DateTime.UtcNow,
            Comment=input.Comment?.Trim()});
        audit.Record(actor,facility,"Transfusion",id,"SignedOff");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return Outcome<TransfusionView>.Ok(await ViewAsync(t,ct));
    }
}
