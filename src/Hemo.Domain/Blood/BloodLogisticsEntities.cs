namespace Hemo.Domain.Blood;

public sealed class UnitIssue
{
    public long UnitIssueId { get; set; }
    public long BloodUnitId { get; set; }
    public long BloodRequestItemId { get; set; }
    public string IssueNo { get; set; } = "";
    public long IssuedByUserId { get; set; }
    public long IssuedToDepartmentId { get; set; }
    public DateTime IssuedAt { get; set; }
    public long? ReceivedByUserId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = "Issued";
    public string? TransportConditionNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class Dispatch
{
    public long DispatchId { get; set; }
    public long FacilityId { get; set; }
    public long DepartmentId { get; set; }
    public string DispatchNo { get; set; } = "";
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtDispatch { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public long? CourierUserId { get; set; }
    public long? ReceiverUserId { get; set; }
    public string? ContainerType { get; set; }
    public decimal? TemperatureAtExitC { get; set; }
    public string Status { get; set; } = "Pending";
    public string? Note { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class DispatchItem
{
    public long DispatchItemId { get; set; }
    public long DispatchId { get; set; }
    public long UnitIssueId { get; set; }
    public decimal? TemperatureAtHandoverC { get; set; }
    public string? ConditionNote { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class UnitReturn
{
    public long UnitReturnId { get; set; }
    public long UnitIssueId { get; set; }
    public long BloodUnitId { get; set; }
    public long ReturnedByUserId { get; set; }
    public long ReceivedByUserId { get; set; }
    public DateTime ReturnedAt { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? ConditionCode { get; set; }
    public string? Decision { get; set; }
    public long? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string Status { get; set; } = "Pending";
    public decimal? TemperatureC { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class UnitWaste
{
    public long UnitWasteId { get; set; }
    public long BloodUnitId { get; set; }
    public long? UnitReturnId { get; set; }
    public string WasteCode { get; set; } = "";
    public string ReasonCode { get; set; } = "";
    public DateTime WasteDate { get; set; }
    public long RecordedByUserId { get; set; }
    public long? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? DisposalMethod { get; set; }
    public string? Description { get; set; }
}
