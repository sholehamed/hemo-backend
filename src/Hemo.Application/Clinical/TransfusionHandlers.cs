using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Clinical;

public sealed class ListTransfusionsHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<ListTransfusionsQuery,Outcome<IReadOnlyList<TransfusionView>>>
{
    public async Task<Outcome<IReadOnlyList<TransfusionView>>> Handle(ListTransfusionsQuery q,CancellationToken ct)=>
        !await permissions.CanAsync(q.Actor,"transfusion.read",q.FacilityId,q.DepartmentId,ct)
        ? Outcome<IReadOnlyList<TransfusionView>>.Fail("forbidden")
        : Outcome<IReadOnlyList<TransfusionView>>.Ok(await store.ListAsync(q.FacilityId,q.DepartmentId,ct));
}
public sealed class GetTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<GetTransfusionQuery,Outcome<TransfusionDetails>>
{
    public async Task<Outcome<TransfusionDetails>> Handle(GetTransfusionQuery q,CancellationToken ct)=>
        !await permissions.CanAsync(q.Actor,"transfusion.read",q.FacilityId,q.DepartmentId,ct)
        ? Outcome<TransfusionDetails>.Fail("forbidden")
        : await store.GetAsync(q.FacilityId,q.DepartmentId,q.Id,ct);
}
public sealed class PrepareTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<PrepareTransfusionCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(PrepareTransfusionCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.start",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.PrepareAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Input,ct);
}
public sealed class RecordChecklistHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<RecordChecklistCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(RecordChecklistCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.start",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.ChecklistAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,ct);
}
public sealed class RecordVitalHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<RecordVitalCommand,Outcome<VitalView>>
{
    public async Task<Outcome<VitalView>> Handle(RecordVitalCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.monitor",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<VitalView>.Fail("forbidden")
        : await store.VitalAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,ct);
}
public sealed class StartTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<StartTransfusionCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(StartTransfusionCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.start",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.TransitionAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,"Start",ct);
}
public sealed class StopTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<StopTransfusionCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(StopTransfusionCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.monitor",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.TransitionAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,"Stop",ct);
}
public sealed class CompleteTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<CompleteTransfusionCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(CompleteTransfusionCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.complete",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.TransitionAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,"Complete",ct);
}
public sealed class SignoffTransfusionHandler(ITransfusionStore store,IResourcePermission permissions)
    : IRequestHandler<SignoffTransfusionCommand,Outcome<TransfusionView>>
{
    public async Task<Outcome<TransfusionView>> Handle(SignoffTransfusionCommand c,CancellationToken ct)=>
        !await permissions.CanAsync(c.Actor,"transfusion.complete",c.FacilityId,c.DepartmentId,ct)
        ? Outcome<TransfusionView>.Fail("forbidden")
        : await store.SignoffAsync(c.Actor,c.FacilityId,c.DepartmentId,c.Id,c.Input,ct);
}
