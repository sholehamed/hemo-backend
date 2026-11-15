using Hemo.Application.Cqrs;

namespace Hemo.Application.Clinical;

public sealed record TransfusionView(long Id,long EncounterId,long BloodRequestItemId,
    long UnitId,long? IssueId,string Status,DateTime? StartedAt,DateTime? EndedAt,
    string? StopReason,string RowVersion,bool ChecklistComplete,bool BaselineRecorded,
    bool SignedOff);
public sealed record VitalView(long Id,long TransfusionId,DateTime ObservedAt,
    string TimepointCode,short? SystolicBp,short? DiastolicBp,short? HeartRate,
    decimal? TemperatureC,byte? Spo2,short? RespiratoryRate,bool AlertFlag,string? ClinicalNote);
public sealed record TransfusionDetails(TransfusionView Transfusion,
    IReadOnlyList<VitalView> Observations,IReadOnlyList<string> Actions);
public sealed record PrepareTransfusionRequest(long IssueId);
public sealed record ChecklistRequest(bool PatientIdentityMatched,bool UnitIdentityMatched,
    bool BloodGroupChecked,bool CompatibilityChecked,bool ExpiryChecked,
    bool BaselineVitalChecked,string? Note);
public sealed record VitalRequest(string TimepointCode,DateTime ObservedAt,
    short? SystolicBp,short? DiastolicBp,short? HeartRate,decimal? TemperatureC,
    byte? Spo2,short? RespiratoryRate,decimal? UrineOutputMl,
    bool AlertFlag,string? ClinicalNote);
public sealed record TransitionRequest(string RowVersion,string? Reason);
public sealed record SignoffRequest(string RowVersion,string? Comment);

public sealed record ListTransfusionsQuery(long Actor,long FacilityId,long DepartmentId)
    : IQuery<Outcome<IReadOnlyList<TransfusionView>>>;
public sealed record GetTransfusionQuery(long Actor,long FacilityId,long DepartmentId,long Id)
    : IQuery<Outcome<TransfusionDetails>>;
public sealed record PrepareTransfusionCommand(long Actor,long FacilityId,long DepartmentId,
    PrepareTransfusionRequest Input):ICommand<Outcome<TransfusionView>>;
public sealed record RecordChecklistCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,ChecklistRequest Input):ICommand<Outcome<TransfusionView>>;
public sealed record RecordVitalCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,VitalRequest Input):ICommand<Outcome<VitalView>>;
public sealed record StartTransfusionCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,TransitionRequest Input):ICommand<Outcome<TransfusionView>>;
public sealed record StopTransfusionCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,TransitionRequest Input):ICommand<Outcome<TransfusionView>>;
public sealed record CompleteTransfusionCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,TransitionRequest Input):ICommand<Outcome<TransfusionView>>;
public sealed record SignoffTransfusionCommand(long Actor,long FacilityId,long DepartmentId,
    long Id,SignoffRequest Input):ICommand<Outcome<TransfusionView>>;

public interface ITransfusionStore
{
    Task<IReadOnlyList<TransfusionView>> ListAsync(long facility,long department,CancellationToken ct);
    Task<Outcome<TransfusionDetails>> GetAsync(long facility,long department,long id,CancellationToken ct);
    Task<Outcome<TransfusionView>> PrepareAsync(long actor,long facility,long department,
        PrepareTransfusionRequest input,CancellationToken ct);
    Task<Outcome<TransfusionView>> ChecklistAsync(long actor,long facility,long department,
        long id,ChecklistRequest input,CancellationToken ct);
    Task<Outcome<VitalView>> VitalAsync(long actor,long facility,long department,
        long id,VitalRequest input,CancellationToken ct);
    Task<Outcome<TransfusionView>> TransitionAsync(long actor,long facility,long department,
        long id,TransitionRequest input,string action,CancellationToken ct);
    Task<Outcome<TransfusionView>> SignoffAsync(long actor,long facility,long department,
        long id,SignoffRequest input,CancellationToken ct);
}
