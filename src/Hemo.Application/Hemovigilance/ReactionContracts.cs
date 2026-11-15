using Hemo.Application.Cqrs;

namespace Hemo.Application.Hemovigilance;

public sealed record ReactionView(long Id,long TransfusionId,long EncounterId,
    string EventNo,string Status,DateTime DetectedAt,bool TransfusionStopped,
    string? SymptomsSummary,string RowVersion,bool Assessed,bool Investigated,long? RcaId);
public sealed record ReactionDetails(ReactionView Event,IReadOnlyList<string> Symptoms,
    IReadOnlyList<string> Assessments,IReadOnlyList<string> Investigations,
    IReadOnlyList<CorrectiveActionView> Actions);
public sealed record RcaView(long Id,long EventId,string Status,string CaseNo,string RowVersion);
public sealed record CorrectiveActionView(long Id,long RcaId,string Title,string Status,
    long AssignedToUserId,string RowVersion);

public sealed record ReportReactionRequest(long TransfusionId,DateTime DetectedAt,
    bool TransfusionStopped,string SymptomsSummary,string ImmediateActions,
    string? Narrative,IReadOnlyList<string> SymptomCodes);
public sealed record PhysicianAssessmentRequest(string DiagnosisText,byte? SeverityGrade,
    string? Causality,string? Interventions,string? Recommendations,string? Outcome,
    bool NationalReportRequired,string RowVersion);
public sealed record InvestigationRequest(string Type,string ResultText,string RowVersion);
public sealed record ReviewReactionRequest(string Decision,string RowVersion,string? Reason);
public sealed record RcaCauseRequest(string CauseType,string Description,bool IsRootCause);
public sealed record CorrectiveActionRequest(string Title,string? Description,
    long AssignedToUserId,DateTime? DueAt);
public sealed record ActionStateRequest(string RowVersion,string? Note);
public sealed record CloseRcaRequest(string RowVersion,string RootCauseSummary);

public sealed record ListReactionsQuery(long Actor,long Facility,long Department)
    : IQuery<Outcome<IReadOnlyList<ReactionView>>>;
public sealed record GetReactionQuery(long Actor,long Facility,long Department,long Id)
    : IQuery<Outcome<ReactionDetails>>;
public sealed record ReportReactionCommand(long Actor,long Facility,long Department,
    ReportReactionRequest Input):ICommand<Outcome<ReactionView>>;
public sealed record AssessReactionCommand(long Actor,long Facility,long Department,long Id,
    PhysicianAssessmentRequest Input):ICommand<Outcome<ReactionView>>;
public sealed record InvestigateReactionCommand(long Actor,long Facility,long Id,
    InvestigationRequest Input):ICommand<Outcome<ReactionView>>;
public sealed record ReviewReactionCommand(long Actor,long Facility,long Id,
    ReviewReactionRequest Input):ICommand<Outcome<ReactionView>>;
public sealed record OpenRcaCommand(long Actor,long Facility,long EventId)
    : ICommand<Outcome<RcaView>>;
public sealed record AddRcaCauseCommand(long Actor,long Facility,long RcaId,
    RcaCauseRequest Input):ICommand<Outcome<RcaView>>;
public sealed record AssignCorrectiveActionCommand(long Actor,long Facility,long RcaId,
    CorrectiveActionRequest Input):ICommand<Outcome<CorrectiveActionView>>;
public sealed record CompleteCorrectiveActionCommand(long Actor,long Facility,long ActionId,
    ActionStateRequest Input):ICommand<Outcome<CorrectiveActionView>>;
public sealed record VerifyCorrectiveActionCommand(long Actor,long Facility,long ActionId,
    ActionStateRequest Input):ICommand<Outcome<CorrectiveActionView>>;
public sealed record CloseRcaCommand(long Actor,long Facility,long RcaId,CloseRcaRequest Input)
    : ICommand<Outcome<RcaView>>;

public interface IReactionStore
{
    Task<IReadOnlyList<ReactionView>> ListAsync(long facility,long department,CancellationToken ct);
    Task<Outcome<ReactionDetails>> GetAsync(long facility,long department,long id,CancellationToken ct);
    Task<Outcome<ReactionView>> ReportAsync(long actor,long facility,long department,
        ReportReactionRequest input,CancellationToken ct);
    Task<Outcome<ReactionView>> AssessAsync(long actor,long facility,long department,long id,
        PhysicianAssessmentRequest input,CancellationToken ct);
    Task<Outcome<ReactionView>> InvestigateAsync(long actor,long facility,long id,
        InvestigationRequest input,CancellationToken ct);
    Task<Outcome<ReactionView>> ReviewAsync(long actor,long facility,long id,
        ReviewReactionRequest input,CancellationToken ct);
    Task<Outcome<RcaView>> OpenRcaAsync(long actor,long facility,long eventId,CancellationToken ct);
    Task<Outcome<RcaView>> CauseAsync(long actor,long facility,long rcaId,
        RcaCauseRequest input,CancellationToken ct);
    Task<Outcome<CorrectiveActionView>> AssignAsync(long actor,long facility,long rcaId,
        CorrectiveActionRequest input,CancellationToken ct);
    Task<Outcome<CorrectiveActionView>> CompleteActionAsync(long actor,long facility,
        long actionId,ActionStateRequest input,CancellationToken ct);
    Task<Outcome<CorrectiveActionView>> VerifyActionAsync(long actor,long facility,
        long actionId,ActionStateRequest input,CancellationToken ct);
    Task<Outcome<RcaView>> CloseRcaAsync(long actor,long facility,long rcaId,
        CloseRcaRequest input,CancellationToken ct);
}
