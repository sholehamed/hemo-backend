using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;

namespace Hemo.Application.Organization;

public sealed class ListAllFacilitiesHandler(IOrganizationStore store)
    : IRequestHandler<ListAllFacilitiesQuery, IReadOnlyList<OrgItem>>
{
    public Task<IReadOnlyList<OrgItem>> Handle(ListAllFacilitiesQuery query, CancellationToken ct) =>
        store.ListAllFacilitiesAsync(ct);
}
public sealed class ListScopedFacilitiesHandler(IOrganizationStore store)
    : IRequestHandler<ListScopedFacilitiesQuery, IReadOnlyList<OrgItem>>
{
    public Task<IReadOnlyList<OrgItem>> Handle(ListScopedFacilitiesQuery query, CancellationToken ct) =>
        store.ListScopedFacilitiesAsync(query.ActorId, ct);
}
public sealed class ListDepartmentsHandler(IOrganizationStore store)
    : IRequestHandler<ListDepartmentsQuery, IReadOnlyList<OrgItem>>
{
    public Task<IReadOnlyList<OrgItem>> Handle(ListDepartmentsQuery query, CancellationToken ct) =>
        store.ListDepartmentsAsync(query.ActorId, query.FacilityId, ct);
}
public sealed class ListAvailableBedsHandler(IOrganizationStore store, IResourcePermission access)
    : IRequestHandler<ListAvailableBedsQuery, Outcome<IReadOnlyList<OrgItem>>>
{
    public async Task<Outcome<IReadOnlyList<OrgItem>>> Handle(
        ListAvailableBedsQuery query, CancellationToken ct)
    {
        if (!await access.CanAsync(query.ActorId, "patient.read", query.FacilityId,
            query.DepartmentId, ct))
            return Outcome<IReadOnlyList<OrgItem>>.Fail("forbidden");
        return Outcome<IReadOnlyList<OrgItem>>.Ok(await store.ListAvailableBedsAsync(
            query.FacilityId, query.DepartmentId, ct));
    }
}
public sealed class CreateFacilityHandler(IOrganizationStore store)
    : IRequestHandler<CreateFacilityCommand, Outcome<OrgItem>>
{
    public Task<Outcome<OrgItem>> Handle(CreateFacilityCommand cmd, CancellationToken ct) =>
        store.CreateFacilityAsync(cmd.ActorId, cmd.Input, ct);
}
public sealed class CreateDepartmentHandler(IOrganizationStore store)
    : IRequestHandler<CreateDepartmentCommand, Outcome<OrgItem>>
{
    public Task<Outcome<OrgItem>> Handle(CreateDepartmentCommand cmd, CancellationToken ct) =>
        store.CreateDepartmentAsync(cmd.ActorId, cmd.Input, ct);
}
public sealed class CreateBedHandler(IOrganizationStore store)
    : IRequestHandler<CreateBedCommand, Outcome<OrgItem>>
{
    public Task<Outcome<OrgItem>> Handle(CreateBedCommand cmd, CancellationToken ct) =>
        store.CreateBedAsync(cmd.ActorId, cmd.Input, ct);
}
