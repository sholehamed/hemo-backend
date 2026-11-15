using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Blood;

public sealed class ListStockHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<ListStockQuery,Outcome<IReadOnlyList<InventoryUnitView>>>
{
    public async Task<Outcome<IReadOnlyList<InventoryUnitView>>> Handle(ListStockQuery q,CancellationToken ct) =>
        !await access.CanAsync(q.Actor,"inventory.read",q.FacilityId,null,ct)
        ? Outcome<IReadOnlyList<InventoryUnitView>>.Fail("forbidden")
        : Outcome<IReadOnlyList<InventoryUnitView>>.Ok(await store.ListAsync(q.FacilityId,ct));
}
public sealed class ListBloodGroupsHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<ListBloodGroupsQuery,Outcome<IReadOnlyList<BloodGroupOption>>>
{
    public async Task<Outcome<IReadOnlyList<BloodGroupOption>>> Handle(ListBloodGroupsQuery q,CancellationToken ct) =>
        !await access.CanAsync(q.Actor,"inventory.read",q.FacilityId,null,ct)
        ? Outcome<IReadOnlyList<BloodGroupOption>>.Fail("forbidden")
        : Outcome<IReadOnlyList<BloodGroupOption>>.Ok(await store.GroupsAsync(ct));
}
public sealed class ReceiveStockHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<ReceiveStockCommand,Outcome<InventoryUnitView>>
{
    public async Task<Outcome<InventoryUnitView>> Handle(ReceiveStockCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"inventory.receive",c.FacilityId,null,ct)
        ? Outcome<InventoryUnitView>.Fail("forbidden")
        : await store.ReceiveAsync(c.Actor,c.FacilityId,c.Input,ct);
}
public sealed class ReserveStockHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<ReserveStockCommand,Outcome<long>>
{
    public async Task<Outcome<long>> Handle(ReserveStockCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"inventory.reserve",c.FacilityId,null,ct)
        ? Outcome<long>.Fail("forbidden")
        : await store.ReserveAsync(c.Actor,c.FacilityId,c.Input,ct);
}
public sealed class RecordCrossmatchHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<RecordCrossmatchCommand,Outcome<CrossmatchView>>
{
    public async Task<Outcome<CrossmatchView>> Handle(RecordCrossmatchCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"crossmatch.perform",c.FacilityId,null,ct)
        ? Outcome<CrossmatchView>.Fail("forbidden")
        : await store.RecordAsync(c.Actor,c.FacilityId,c.Input,ct);
}
public sealed class FinalizeCrossmatchHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<FinalizeCrossmatchCommand,Outcome<CrossmatchView>>
{
    public async Task<Outcome<CrossmatchView>> Handle(FinalizeCrossmatchCommand c,CancellationToken ct) =>
        !await access.CanAsync(c.Actor,"crossmatch.approve",c.FacilityId,null,ct)
        ? Outcome<CrossmatchView>.Fail("forbidden")
        : await store.FinalizeAsync(c.Actor,c.FacilityId,c.CrossmatchId,c.Input,ct);
}

public sealed class ListBankWorkHandler(IInventoryStore store,IResourcePermission access)
    : IRequestHandler<ListBankWorkQuery,Outcome<IReadOnlyList<BankWorkItem>>>
{
    public async Task<Outcome<IReadOnlyList<BankWorkItem>>> Handle(ListBankWorkQuery q,CancellationToken ct) =>
        !await access.CanAsync(q.Actor,"inventory.read",q.FacilityId,null,ct)
        ? Outcome<IReadOnlyList<BankWorkItem>>.Fail("forbidden")
        : Outcome<IReadOnlyList<BankWorkItem>>.Ok(await store.WorkAsync(q.FacilityId,ct));
}
