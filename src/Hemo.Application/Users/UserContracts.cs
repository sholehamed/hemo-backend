using Hemo.Application.Cqrs;

namespace Hemo.Application.Users;

public sealed record ManagedUser(long Id, string UserName, string DisplayName, bool IsActive);
public sealed record ManagedRole(long RoleId, string Code, string Name);
public sealed record CreateUserRequest(string UserName, string DisplayName, string InitialPassword);
public sealed record GrantUserRoleRequest(long RoleId, long? FacilityId, long? DepartmentId);
public sealed record GrantReceipt(long UserRoleId);

public sealed record ListManagedUsersQuery(string? Search, int Page)
    : IQuery<Outcome<IReadOnlyList<ManagedUser>>>;
public sealed record ListManagedRolesQuery() : IQuery<IReadOnlyList<ManagedRole>>;
public sealed record CreateManagedUserCommand(long ActorId, CreateUserRequest Input)
    : ICommand<Outcome<ManagedUser>>;
public sealed record GrantManagedRoleCommand(long ActorId, long UserId, GrantUserRoleRequest Input)
    : ICommand<Outcome<GrantReceipt>>;
public sealed record DeactivateManagedUserCommand(long ActorId, long UserId)
    : ICommand<Outcome<bool>>;

/// <summary>
/// Credential hashing, EF persistence, audit and session revocation are infrastructure concerns.
/// NEVER log or serialize the initial password from the create request.
/// </summary>
public interface IUserAdministrationStore
{
    Task<Outcome<IReadOnlyList<ManagedUser>>> ListAsync(
        string? search, int page, CancellationToken ct);
    Task<IReadOnlyList<ManagedRole>> ListRolesAsync(CancellationToken ct);
    Task<Outcome<ManagedUser>> CreateAsync(long actor, CreateUserRequest input,
        CancellationToken ct);
    Task<Outcome<GrantReceipt>> GrantAsync(long actor, long userId,
        GrantUserRoleRequest input, CancellationToken ct);
    Task<Outcome<bool>> DeactivateAsync(long actor, long userId, CancellationToken ct);
}
