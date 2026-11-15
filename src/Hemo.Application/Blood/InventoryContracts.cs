using Hemo.Application.Cqrs;

namespace Hemo.Application.Blood;

public sealed record BloodGroupOption(long Id, string Code);
public sealed record InventoryUnitView(long Id,string UnitCode,long ProductTypeId,long BloodGroupId,
    long FacilityId,DateOnly ExpiryDate,string Status,bool IsQuarantined,string RowVersion);
public sealed record CrossmatchView(long Id,long UnitId,long RequestItemId,string Status,
    string CompatibilityResult,long PerformedBy,long? ApprovedBy,string RowVersion);
public sealed record RegisterUnitRequest(string UnitCode,long ProductTypeId,long BloodGroupId,
    DateOnly ExpiryDate,string? StorageLocation);
public sealed record ReserveUnitRequest(long UnitId,long RequestId);
public sealed record RecordCrossmatchRequest(long ReservationId,string CompatibilityResult,
    string TestCode,string TestResult,string? Comments);
public sealed record FinalizeCrossmatchRequest(string RowVersion);

public sealed record ListStockQuery(long Actor,long FacilityId) : IQuery<Outcome<IReadOnlyList<InventoryUnitView>>>;
public sealed record ListBloodGroupsQuery(long Actor,long FacilityId) : IQuery<Outcome<IReadOnlyList<BloodGroupOption>>>;
public sealed record ReceiveStockCommand(long Actor,long FacilityId,RegisterUnitRequest Input) : ICommand<Outcome<InventoryUnitView>>;
public sealed record ReserveStockCommand(long Actor,long FacilityId,ReserveUnitRequest Input) : ICommand<Outcome<long>>;
public sealed record RecordCrossmatchCommand(long Actor,long FacilityId,RecordCrossmatchRequest Input) : ICommand<Outcome<CrossmatchView>>;
public sealed record FinalizeCrossmatchCommand(long Actor,long FacilityId,long CrossmatchId,FinalizeCrossmatchRequest Input) : ICommand<Outcome<CrossmatchView>>;

public interface IInventoryStore
{
    Task<IReadOnlyList<InventoryUnitView>> ListAsync(long facility,CancellationToken ct);
    Task<IReadOnlyList<BloodGroupOption>> GroupsAsync(CancellationToken ct);
    Task<Outcome<InventoryUnitView>> ReceiveAsync(long actor,long facility,RegisterUnitRequest input,CancellationToken ct);
    Task<Outcome<long>> ReserveAsync(long actor,long facility,ReserveUnitRequest input,CancellationToken ct);
    Task<Outcome<CrossmatchView>> RecordAsync(long actor,long facility,RecordCrossmatchRequest input,CancellationToken ct);
    Task<Outcome<CrossmatchView>> FinalizeAsync(long actor,long facility,long crossmatchId,FinalizeCrossmatchRequest input,CancellationToken ct);
}
