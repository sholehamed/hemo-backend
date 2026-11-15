namespace Hemo.Domain.Clinical;

public sealed class Transfusion
{
    public long TransfusionId { get; set; }
    public long EncounterId { get; set; }
    public long BloodRequestItemId { get; set; }
    public long BloodUnitId { get; set; }
    public long? StartedByUserId { get; set; }
    public long? EndedByUserId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Status { get; set; } = "Planned";
    public string? StopReason { get; set; }
    public string? FinalDisposition { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class TransfusionChecklist
{
    public long TransfusionChecklistId { get; set; }
    public long TransfusionId { get; set; }
    public bool PatientIdentityMatched { get; set; }
    public bool UnitIdentityMatched { get; set; }
    public bool BloodGroupChecked { get; set; }
    public bool CompatibilityChecked { get; set; }
    public bool ExpiryChecked { get; set; }
    public bool BaselineVitalChecked { get; set; }
    public long CheckedByUserId { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class VitalObservation
{
    public long VitalObservationId { get; set; }
    public long TransfusionId { get; set; }
    public DateTime ObservedAt { get; set; }
    public string TimepointCode { get; set; } = "";
    public short? SystolicBp { get; set; }
    public short? DiastolicBp { get; set; }
    public short? HeartRate { get; set; }
    public decimal? TemperatureC { get; set; }
    public byte? Spo2 { get; set; }
    public short? RespiratoryRate { get; set; }
    public decimal? UrineOutputMl { get; set; }
    public long RecordedByUserId { get; set; }
    public string? ClinicalNote { get; set; }
    public bool AlertFlag { get; set; }
}
public sealed class TransfusionAction
{
    public long TransfusionActionId { get; set; }
    public long TransfusionId { get; set; }
    public string ActionType { get; set; } = "";
    public DateTime ActionAt { get; set; }
    public long PerformedByUserId { get; set; }
    public string? Description { get; set; }
}
public sealed class TransfusionSignoff
{
    public long TransfusionSignoffId { get; set; }
    public long TransfusionId { get; set; }
    public long SignerUserId { get; set; }
    public string SignoffRole { get; set; } = "";
    public DateTime SignedAt { get; set; }
    public byte[]? SignatureHash { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
