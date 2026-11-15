using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Cqrs;
using Hemo.Application.Hemovigilance;
using Hemo.Domain.Hemovigilance;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Hemovigilance;

/// <summary>
/// Append-only incident evidence and independent physician/HV review.
/// No software-calculated diagnosis, causality, severity or medical treatment.
/// </summary>
public sealed class ReactionSqlStore(HemoDbContext db,HemoAuditWriter audit,
    ResourceAccessService access) : IReactionStore
{
    private IQueryable<AdverseEvent> Scoped(long facility,long department)=>
        from e in db.AdverseEvents
        join transfusion in db.Transfusions on e.TransfusionId equals transfusion.TransfusionId
        join encounter in db.Encounters on transfusion.EncounterId equals encounter.EncounterId
        join location in db.EncounterLocations on encounter.EncounterId equals location.EncounterId
        where encounter.FacilityId==facility && location.DepartmentId==department &&
            location.ToAt==null
        select e;

    private IQueryable<AdverseEvent> ScopedFacility(long facility)=>
        from e in db.AdverseEvents
        join transfusion in db.Transfusions on e.TransfusionId equals transfusion.TransfusionId
        join encounter in db.Encounters on transfusion.EncounterId equals encounter.EncounterId
        where encounter.FacilityId==facility
        select e;

    private IQueryable<RcaCase> ScopedRca(long facility)=>
        from r in db.RcaCases
        join e in db.AdverseEvents on r.AdverseEventId equals e.AdverseEventId
        join t in db.Transfusions on e.TransfusionId equals t.TransfusionId
        join encounter in db.Encounters on t.EncounterId equals encounter.EncounterId
        where encounter.FacilityId==facility
        select r;

    private IQueryable<CorrectiveAction> ScopedAction(long facility)=>
        from a in db.CorrectiveActions
        join r in db.RcaCases on a.RcaCaseId equals r.RcaCaseId
        join e in db.AdverseEvents on r.AdverseEventId equals e.AdverseEventId
        join t in db.Transfusions on e.TransfusionId equals t.TransfusionId
        join encounter in db.Encounters on t.EncounterId equals encounter.EncounterId
        where encounter.FacilityId==facility
        select a;

    private static bool ValidVersion(byte[] version,string? provided)
    {
        try {var candidate=Convert.FromBase64String(provided??"");
            return candidate.Length==version.Length &&
                CryptographicOperations.FixedTimeEquals(candidate,version);
        }catch(FormatException){return false;}
    }
    private static RcaView Rca(RcaCase r)=>new(r.RcaCaseId,r.AdverseEventId??0,
        r.Status,r.CaseNo,Convert.ToBase64String(r.RowVersion));
    private static CorrectiveActionView Action(CorrectiveAction a)=>new(
        a.CorrectiveActionId,a.RcaCaseId??0,a.Title,a.Status,
        a.AssignedToUserId,Convert.ToBase64String(a.RowVersion));

    private async Task<ReactionView> ViewAsync(AdverseEvent e,CancellationToken ct)
    {
        var encounterId=await db.Transfusions.AsNoTracking()
            .Where(t=>t.TransfusionId==e.TransfusionId)
            .Select(t=>t.EncounterId).SingleAsync(ct);
        var assessed=await db.PhysicianEventAssessments.AsNoTracking()
            .AnyAsync(x=>x.AdverseEventId==e.AdverseEventId,ct);
        var investigated=await db.EventInvestigations.AsNoTracking()
            .AnyAsync(x=>x.AdverseEventId==e.AdverseEventId && x.InvestigationType!="HVReview",ct);
        var rca=await db.RcaCases.AsNoTracking().Where(x=>x.AdverseEventId==e.AdverseEventId)
            .Select(x=>(long?)x.RcaCaseId).FirstOrDefaultAsync(ct);
        return new ReactionView(e.AdverseEventId,e.TransfusionId,encounterId,e.EventNo,
            e.Status,e.DetectedAt,e.TransfusionStopped,e.SymptomsSummary,
            Convert.ToBase64String(e.RowVersion),assessed,investigated,rca);
    }

    public async Task<IReadOnlyList<ReactionView>> ListAsync(
        long facility,long department,CancellationToken ct)
    {
        var rows=await Scoped(facility,department).AsNoTracking()
            .OrderByDescending(x=>x.ReportedAt).Take(200).ToArrayAsync(ct);
        var list=new List<ReactionView>();
        foreach(var row in rows)list.Add(await ViewAsync(row,ct));
        return list;
    }

    public async Task<Outcome<ReactionDetails>> GetAsync(
        long facility,long department,long id,CancellationToken ct)
    {
        var e=await Scoped(facility,department).AsNoTracking()
            .SingleOrDefaultAsync(x=>x.AdverseEventId==id,ct);
        if(e is null)return Outcome<ReactionDetails>.Fail("not_found");
        var symptoms=await db.EventSymptoms.AsNoTracking()
            .Where(x=>x.AdverseEventId==id).OrderBy(x=>x.EventSymptomId)
            .Select(x=>x.SymptomCode).ToArrayAsync(ct);
        var assessments=await db.PhysicianEventAssessments.AsNoTracking()
            .Where(x=>x.AdverseEventId==id).OrderBy(x=>x.AssessedAt)
            .Select(x=>x.DiagnosisText).ToArrayAsync(ct);
        var investigations=await db.EventInvestigations.AsNoTracking()
            .Where(x=>x.AdverseEventId==id).OrderBy(x=>x.InvestigationAt)
            .Select(x=>x.InvestigationType).ToArrayAsync(ct);
        var rcaId=await db.RcaCases.AsNoTracking().Where(x=>x.AdverseEventId==id)
            .Select(x=>(long?)x.RcaCaseId).FirstOrDefaultAsync(ct);
        var actions=rcaId.HasValue
            ? await db.CorrectiveActions.AsNoTracking().Where(x=>x.RcaCaseId==rcaId.Value)
                .OrderBy(x=>x.CorrectiveActionId).ToArrayAsync(ct)
            : Array.Empty<CorrectiveAction>();
        return Outcome<ReactionDetails>.Ok(new ReactionDetails(
            await ViewAsync(e,ct),symptoms,assessments,investigations,
            actions.Select(Action).ToArray()));
    }

    public async Task<Outcome<ReactionView>> ReportAsync(long actor,long facility,
        long department,ReportReactionRequest input,CancellationToken ct)
    {
        if(input.TransfusionId<=0||input.DetectedAt.Kind!=DateTimeKind.Utc ||
           input.DetectedAt>DateTime.UtcNow.AddMinutes(5) ||
           input.DetectedAt<DateTime.UtcNow.AddDays(-30) ||
           string.IsNullOrWhiteSpace(input.SymptomsSummary) ||
           input.SymptomsSummary.Length is >1800 or <3 ||
           string.IsNullOrWhiteSpace(input.ImmediateActions) ||
           input.ImmediateActions.Length>1800 ||
           (input.Narrative?.Length??0)>10000 ||
           input.SymptomCodes is null || input.SymptomCodes.Count is <1 or >10 ||
           input.SymptomCodes.Any(x=>string.IsNullOrWhiteSpace(x) || x.Length>80 ||
               x.Any(c=>!(c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '_' or '-'))))
            return Outcome<ReactionView>.Fail("invalid_report");

        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var t=await (from transfusion in db.Transfusions
            join encounter in db.Encounters on transfusion.EncounterId equals encounter.EncounterId
            join loc in db.EncounterLocations on encounter.EncounterId equals loc.EncounterId
            where transfusion.TransfusionId==input.TransfusionId &&
                encounter.FacilityId==facility && loc.DepartmentId==department && loc.ToAt==null
            select transfusion).SingleOrDefaultAsync(ct);
        if(t is null)return Outcome<ReactionView>.Fail("not_found");
        // Software cannot stop an actual bedside infusion. The clinician must physically
        // stop and explicitly record it via ST-08 before creating an in-progress reaction.
        if(t.Status=="InProgress")return Outcome<ReactionView>.Fail("stop_before_report");
        if(t.Status is not ("Stopped" or "Completed") ||
           (t.Status=="Stopped" && !input.TransfusionStopped) ||
           t.StartedAt is null || input.DetectedAt<t.StartedAt.Value.AddHours(-1))
            return Outcome<ReactionView>.Fail("invalid_state");

        var e=new AdverseEvent{TransfusionId=t.TransfusionId,
            EventNo="ADV-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),
            DetectedAt=input.DetectedAt,ReportedAt=DateTime.UtcNow,
            ReportedByUserId=actor,Status="Reported",
            TransfusionStopped=input.TransfusionStopped,
            SymptomsSummary=input.SymptomsSummary.Trim(),
            ImmediateActions=input.ImmediateActions.Trim(),
            Narrative=input.Narrative?.Trim()};
        db.AdverseEvents.Add(e);
        try {
            await db.SaveChangesAsync(ct);
            foreach(var code in input.SymptomCodes.Distinct(StringComparer.Ordinal))
                db.EventSymptoms.Add(new EventSymptom{
                    AdverseEventId=e.AdverseEventId,SymptomCode=code});
            audit.Record(actor,facility,"AdverseEvent",e.AdverseEventId,"Reported");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReactionView>.Ok(await ViewAsync(e,ct));
        }catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<ReactionView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<ReactionView>> AssessAsync(long actor,long facility,long department,
        long id,PhysicianAssessmentRequest input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.DiagnosisText)||
           input.DiagnosisText.Length>1500 || input.SeverityGrade is >4 ||
           (input.Causality?.Length??0)>35 ||
           (input.Interventions?.Length??0)>1400 ||
           (input.Recommendations?.Length??0)>1400 ||
           (input.Outcome?.Length??0)>1400)
            return Outcome<ReactionView>.Fail("invalid_assessment");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var e=await Scoped(facility,department).SingleOrDefaultAsync(x=>x.AdverseEventId==id,ct);
        if(e is null)return Outcome<ReactionView>.Fail("not_found");
        if(e.Status!="Reported"||e.ReportedByUserId==actor||
           await db.PhysicianEventAssessments.AnyAsync(x=>x.AdverseEventId==id,ct))
            return Outcome<ReactionView>.Fail("invalid_state");
        if(!ValidVersion(e.RowVersion,input.RowVersion))
            return Outcome<ReactionView>.Fail("concurrency_conflict");
        db.PhysicianEventAssessments.Add(new PhysicianEventAssessment{
            AdverseEventId=id,AssessedByUserId=actor,AssessedAt=DateTime.UtcNow,
            DiagnosisText=input.DiagnosisText.Trim(),SeverityGrade=input.SeverityGrade,
            Causality=input.Causality?.Trim(),Interventions=input.Interventions?.Trim(),
            Recommendations=input.Recommendations?.Trim(),Outcome=input.Outcome?.Trim(),
            NationalReportRequired=input.NationalReportRequired});
        e.Status="DoctorReview";e.SeverityGrade=input.SeverityGrade;
        audit.Record(actor,facility,"AdverseEvent",id,"PhysicianAssessed");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReactionView>.Ok(await ViewAsync(e,ct));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<ReactionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<ReactionView>> InvestigateAsync(long actor,long facility,long id,
        InvestigationRequest input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Type)||input.Type.Length>50 ||
           string.IsNullOrWhiteSpace(input.ResultText) || input.ResultText.Length>2000)
            return Outcome<ReactionView>.Fail("invalid_investigation");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var e=await ScopedFacility(facility).SingleOrDefaultAsync(x=>x.AdverseEventId==id,ct);
        if(e is null)return Outcome<ReactionView>.Fail("not_found");
        if(e.Status is not ("DoctorReview" or "Investigation") ||
           !await db.PhysicianEventAssessments.AnyAsync(x=>x.AdverseEventId==id,ct))
            return Outcome<ReactionView>.Fail("invalid_state");
        if(!ValidVersion(e.RowVersion,input.RowVersion))
            return Outcome<ReactionView>.Fail("concurrency_conflict");
        db.EventInvestigations.Add(new EventInvestigation{AdverseEventId=id,
            InvestigationType=input.Type.Trim(),InvestigationAt=DateTime.UtcNow,
            PerformedByUserId=actor,ResultText=input.ResultText.Trim()});
        e.Status="Investigation";
        audit.Record(actor,facility,"AdverseEvent",id,"Investigated");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReactionView>.Ok(await ViewAsync(e,ct));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<ReactionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<ReactionView>> ReviewAsync(long actor,long facility,long id,
        ReviewReactionRequest input,CancellationToken ct)
    {
        if(input.Decision is not ("Confirmed" or "Closed")||
           string.IsNullOrWhiteSpace(input.Reason)||input.Reason.Length>1000)
            return Outcome<ReactionView>.Fail("invalid_review");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var e=await ScopedFacility(facility).SingleOrDefaultAsync(x=>x.AdverseEventId==id,ct);
        if(e is null)return Outcome<ReactionView>.Fail("not_found");
        if(!ValidVersion(e.RowVersion,input.RowVersion))
            return Outcome<ReactionView>.Fail("concurrency_conflict");
        if((input.Decision=="Confirmed" && e.Status!="Investigation") ||
           (input.Decision=="Closed" && e.Status!="Confirmed") ||
           !await db.EventInvestigations.AnyAsync(x=>x.AdverseEventId==id &&
                x.InvestigationType!="HVReview",ct))
            return Outcome<ReactionView>.Fail("invalid_state");
        if(input.Decision=="Closed" &&
           await db.RcaCases.AnyAsync(x=>x.AdverseEventId==id && x.Status!="Closed",ct))
            return Outcome<ReactionView>.Fail("rca_not_closed");

        e.Status=input.Decision;
        e.Outcome=input.Reason.Trim();
        db.EventInvestigations.Add(new EventInvestigation{AdverseEventId=id,
            InvestigationType="HVReview",InvestigationAt=DateTime.UtcNow,
            PerformedByUserId=actor,ResultText=input.Reason.Trim()});
        audit.Record(actor,facility,"AdverseEvent",id,input.Decision);
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<ReactionView>.Ok(await ViewAsync(e,ct));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<ReactionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<RcaView>> OpenRcaAsync(long actor,long facility,long eventId,
        CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var e=await ScopedFacility(facility).AsNoTracking()
            .SingleOrDefaultAsync(x=>x.AdverseEventId==eventId,ct);
        if(e is null)return Outcome<RcaView>.Fail("not_found");
        if(e.Status is not ("Investigation" or "Confirmed") ||
           !await db.PhysicianEventAssessments.AnyAsync(x=>x.AdverseEventId==eventId,ct))
            return Outcome<RcaView>.Fail("investigation_required");
        if(await db.RcaCases.AnyAsync(x=>x.AdverseEventId==eventId,ct))
            return Outcome<RcaView>.Fail("rca_exists");
        var r=new RcaCase{AdverseEventId=eventId,
            CaseNo="RCA-"+Guid.NewGuid().ToString("N").ToUpperInvariant(),
            OpenedAt=DateTime.UtcNow,OpenedByUserId=actor,Status="Open"};
        db.RcaCases.Add(r);
        try{await db.SaveChangesAsync(ct);
            audit.Record(actor,facility,"RcaCase",r.RcaCaseId,"Opened");
            await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<RcaView>.Ok(Rca(r));}
        catch(DbUpdateException){await tx.RollbackAsync(ct);
            return Outcome<RcaView>.Fail("constraint_conflict");}
    }

    public async Task<Outcome<RcaView>> CauseAsync(long actor,long facility,long rcaId,
        RcaCauseRequest input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.CauseType)||input.CauseType.Length>50 ||
           string.IsNullOrWhiteSpace(input.Description)||input.Description.Length>1200)
            return Outcome<RcaView>.Fail("invalid_cause");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var r=await ScopedRca(facility).SingleOrDefaultAsync(x=>x.RcaCaseId==rcaId,ct);
        if(r is null)return Outcome<RcaView>.Fail("not_found");
        if(r.Status is not ("Open" or "Analyzing"))
            return Outcome<RcaView>.Fail("invalid_state");
        db.RcaCauses.Add(new RcaCause{RcaCaseId=rcaId,CauseType=input.CauseType.Trim(),
            Description=input.Description.Trim(),IsRootCause=input.IsRootCause});
        r.Status="Analyzing";
        audit.Record(actor,facility,"RcaCase",rcaId,"CauseAdded");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return Outcome<RcaView>.Ok(Rca(r));
    }

    public async Task<Outcome<CorrectiveActionView>> AssignAsync(long actor,long facility,
        long rcaId,CorrectiveActionRequest input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.Title)||input.Title.Length>250||
           (input.Description?.Length??0)>2000 || input.AssignedToUserId<=0 ||
           (input.DueAt.HasValue && (input.DueAt.Value.Kind!=DateTimeKind.Utc ||
                input.DueAt.Value<DateTime.UtcNow)))
            return Outcome<CorrectiveActionView>.Fail("invalid_action");
        if(!await access.CanAsync(input.AssignedToUserId,"rca.manage",facility,null,ct))
            return Outcome<CorrectiveActionView>.Fail("invalid_assignee");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var r=await ScopedRca(facility).SingleOrDefaultAsync(x=>x.RcaCaseId==rcaId,ct);
        if(r is null)return Outcome<CorrectiveActionView>.Fail("not_found");
        if(r.Status is not ("Analyzing" or "ActionPending") ||
           !await db.RcaCauses.AnyAsync(x=>x.RcaCaseId==rcaId && x.IsRootCause,ct))
            return Outcome<CorrectiveActionView>.Fail("root_cause_required");
        var a=new CorrectiveAction{RcaCaseId=rcaId,Title=input.Title.Trim(),
            Description=input.Description?.Trim(),AssignedToUserId=input.AssignedToUserId,
            DueAt=input.DueAt,Status="Open"};
        db.CorrectiveActions.Add(a);r.Status="ActionPending";
        await db.SaveChangesAsync(ct);
        audit.Record(actor,facility,"CorrectiveAction",a.CorrectiveActionId,"Assigned");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return Outcome<CorrectiveActionView>.Ok(Action(a));
    }

    public async Task<Outcome<CorrectiveActionView>> CompleteActionAsync(
        long actor,long facility,long actionId,ActionStateRequest input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var a=await ScopedAction(facility).SingleOrDefaultAsync(x=>x.CorrectiveActionId==actionId,ct);
        if(a is null)return Outcome<CorrectiveActionView>.Fail("not_found");
        if(!ValidVersion(a.RowVersion,input.RowVersion))
            return Outcome<CorrectiveActionView>.Fail("concurrency_conflict");
        if(a.Status is not ("Open" or "InProgress")||a.AssignedToUserId!=actor)
            return Outcome<CorrectiveActionView>.Fail("assigned_actor_required");
        a.Status="Completed";a.CompletedAt=DateTime.UtcNow;
        audit.Record(actor,facility,"CorrectiveAction",actionId,"Completed");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<CorrectiveActionView>.Ok(Action(a));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<CorrectiveActionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<CorrectiveActionView>> VerifyActionAsync(
        long actor,long facility,long actionId,ActionStateRequest input,CancellationToken ct)
    {
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var a=await ScopedAction(facility).SingleOrDefaultAsync(x=>x.CorrectiveActionId==actionId,ct);
        if(a is null)return Outcome<CorrectiveActionView>.Fail("not_found");
        if(!ValidVersion(a.RowVersion,input.RowVersion))
            return Outcome<CorrectiveActionView>.Fail("concurrency_conflict");
        if(a.Status!="Completed"||a.AssignedToUserId==actor)
            return Outcome<CorrectiveActionView>.Fail("independent_verification_required");
        a.Status="Verified";a.VerifiedByUserId=actor;a.VerifiedAt=DateTime.UtcNow;
        audit.Record(actor,facility,"CorrectiveAction",actionId,"Verified");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<CorrectiveActionView>.Ok(Action(a));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<CorrectiveActionView>.Fail("concurrency_conflict");}
    }

    public async Task<Outcome<RcaView>> CloseRcaAsync(long actor,long facility,long rcaId,
        CloseRcaRequest input,CancellationToken ct)
    {
        if(string.IsNullOrWhiteSpace(input.RootCauseSummary)||
           input.RootCauseSummary.Length>5000)
            return Outcome<RcaView>.Fail("invalid_closure");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var r=await ScopedRca(facility).SingleOrDefaultAsync(x=>x.RcaCaseId==rcaId,ct);
        if(r is null)return Outcome<RcaView>.Fail("not_found");
        if(!ValidVersion(r.RowVersion,input.RowVersion))
            return Outcome<RcaView>.Fail("concurrency_conflict");
        if(r.Status!="ActionPending"||r.OpenedByUserId==actor ||
           !await db.RcaCauses.AnyAsync(x=>x.RcaCaseId==rcaId && x.IsRootCause,ct))
            return Outcome<RcaView>.Fail("independent_closure_required");
        var actions=await db.CorrectiveActions.AsNoTracking()
            .Where(x=>x.RcaCaseId==rcaId).Select(x=>x.Status).ToArrayAsync(ct);
        if(actions.Length==0||actions.Any(x=>x!="Verified"))
            return Outcome<RcaView>.Fail("actions_not_verified");
        r.Status="Closed";r.ClosedAt=DateTime.UtcNow;
        r.RootCauseSummary=input.RootCauseSummary.Trim();
        audit.Record(actor,facility,"RcaCase",rcaId,"Closed");
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
            return Outcome<RcaView>.Ok(Rca(r));}
        catch(DbUpdateConcurrencyException){await tx.RollbackAsync(ct);
            return Outcome<RcaView>.Fail("concurrency_conflict");}
    }
}
