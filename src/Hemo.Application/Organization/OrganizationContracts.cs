using Hemo.Application.Cqrs;

namespace Hemo.Application.Organization;

public sealed record OrgItem(long Id, string Code, string Name, long? ParentId);
public sealed record NewFacilityRequest(string Code, string Name);
public sealed record NewDepartmentRequest(long FacilityId, string Code, string Name,
    long? ParentDepartmentId);
public sealed record NewBedRequest(long DepartmentId, string BedCode);

public sealed record ListAllFacilitiesQuery() : IQuery<IReadOnlyList<OrgItem>>;
public sealed record ListScopedFacilitiesQuery(long ActorId) : IQuery<IReadOnlyList<OrgItem>>;
public sealed record ListDepartmentsQuery(long ActorId, long FacilityId)
    : IQuery<IReadOnlyList<OrgItem>>;
public sealed record ListAvailableBedsQuery(long ActorId, long FacilityId, long DepartmentId)
    : IQuery<Outcome<IReadOnlyList<OrgItem>>>;
public sealed record CreateFacilityCommand(long ActorId, NewFacilityRequest Input)
    : ICommand<Outcome<OrgItem>>;
public sealed record CreateDepartmentCommand(long ActorId, NewDepartmentRequest Input)
    : ICommand<Outcome<OrgItem>>;
public sealed record CreateBedCommand(long ActorId, NewBedRequest Input)
    : ICommand<Outcome<OrgItem>>;

public interface IOrganizationStore
{
    Task<IReadOnlyList<OrgItem>> ListAllFacilitiesAsync(CancellationToken ct);
    Task<IReadOnlyList<OrgItem>> ListScopedFacilitiesAsync(long actor, CancellationToken ct);
    Task<IReadOnlyList<OrgItem>> ListDepartmentsAsync(long actor,
        long facilityId, CancellationToken ct);
    Task<IReadOnlyList<OrgItem>> ListAvailableBedsAsync(long facilityId,
        long departmentId, CancellationToken ct);
    Task<Outcome<OrgItem>> CreateFacilityAsync(long actor, NewFacilityRequest input,
        CancellationToken ct);
    Task<Outcome<OrgItem>> CreateDepartmentAsync(long actor, NewDepartmentRequest input,
        CancellationToken ct);
    Task<Outcome<OrgItem>> CreateBedAsync(long actor, NewBedRequest input,
        CancellationToken ct);
}
