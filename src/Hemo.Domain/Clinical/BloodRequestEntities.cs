namespace Hemo.Domain.Clinical;

public sealed class BloodRequest
{
    public long BloodRequestId { get; set; }
    public long EncounterId { get; set; }
    public string RequestNo { get; set; } = "";
    public long RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; }
    public string RequestSource { get; set; } = "Nurse";
    public string RequestType { get; set; } = "Transfusion";
    public string Priority { get; set; } = "Routine";
    public string Status { get; set; } = "Draft";
    public string? Indication { get; set; }
    public string? Diagnosis { get; set; }
    public DateTime? ExpectedAt { get; set; }
    public string? ExternalRequestNo { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class BloodRequestItem
{
    public long BloodRequestItemId { get; set; }
    public long BloodRequestId { get; set; }
    public long ProductTypeId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string QuantityUnit { get; set; } = "Unit";
    public string? SpecialRequirements { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RequestClinicalData
{
    public long RequestClinicalDataId { get; set; }
    public long BloodRequestId { get; set; }
    public decimal? Hemoglobin { get; set; }
    public decimal? Hematocrit { get; set; }
    public bool? HasPreviousTransfusion { get; set; }
    public bool? HasPreviousReaction { get; set; }
    public bool? HasAntibodyHistory { get; set; }
    public string? PregnancyHistory { get; set; }
    public bool? PremedicationRequired { get; set; }
    public string? PremedicationDetails { get; set; }
    public string? EmergencyReason { get; set; }
    public string? AdditionalDataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class RequestApproval
{
    public long RequestApprovalId { get; set; }
    public long BloodRequestId { get; set; }
    public string ApprovalStage { get; set; } = "Doctor";
    public long ReviewerUserId { get; set; }
    public string Decision { get; set; } = "";
    public DateTime DecidedAt { get; set; }
    public string? Reason { get; set; }
    public int RevisionNo { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class ClinicalTask
{
    public long ClinicalTaskId { get; set; }
    public long FacilityId { get; set; }
    public long? EncounterId { get; set; }
    public long? BloodRequestId { get; set; }
    public long? AssignedUserId { get; set; }
    public long? AssignedRoleId { get; set; }
    public string TaskType { get; set; } = "";
    public string Priority { get; set; } = "Routine";
    public string Status { get; set; } = "Open";
    public DateTime? DueAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? CreatedByUserId { get; set; }
    public string Summary { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
