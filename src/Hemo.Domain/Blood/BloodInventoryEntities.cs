namespace Hemo.Domain.Blood;

public sealed class BloodGroup
{
    public long BloodGroupId { get; set; }
    public string Code { get; set; } = "";
    public string Abo { get; set; } = "";
    public string RhD { get; set; } = "";
    public byte[] RowVersion { get; set; } = [];
}
public sealed class BloodUnit
{
    public long BloodUnitId { get; set; }
    public string UnitCode { get; set; } = "";
    public long ProductTypeId { get; set; }
    public long BloodGroupId { get; set; }
    public long? SupplierId { get; set; }
    public long CurrentFacilityId { get; set; }
    public long? StockReceiptId { get; set; }
    public DateOnly? CollectionDate { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = "PendingReceipt";
    public string? SpecialAttributesJson { get; set; }
    public string? StorageLocation { get; set; }
    public bool IsQuarantined { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class UnitReservation
{
    public long UnitReservationId { get; set; }
    public long BloodUnitId { get; set; }
    public long BloodRequestItemId { get; set; }
    public long ReservedByUserId { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public string Status { get; set; } = "Active";
    public string? ReleaseReason { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class Crossmatch
{
    public long CrossmatchId { get; set; }
    public long BloodRequestItemId { get; set; }
    public long BloodUnitId { get; set; }
    public long PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public long? PatientBloodGroupId { get; set; }
    public long? UnitLabelBloodGroupId { get; set; }
    public long? VerifiedUnitBloodGroupId { get; set; }
    public string? AntibodyScreenResult { get; set; }
    public string CompatibilityResult { get; set; } = "Pending";
    public string Status { get; set; } = "Draft";
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class InventoryMovement
{
    public long InventoryMovementId { get; set; }
    public long BloodUnitId { get; set; }
    public long? FromFacilityId { get; set; }
    public long? ToFacilityId { get; set; }
    public string MovementType { get; set; } = "";
    public DateTime MovementAt { get; set; }
    public long PerformedByUserId { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? PriorStatus { get; set; }
    public string? NewStatus { get; set; }
    public string? Explanation { get; set; }
}
