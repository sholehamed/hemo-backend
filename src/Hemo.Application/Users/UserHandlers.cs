using Hemo.Application.Cqrs;

namespace Hemo.Application.Users;

public sealed class ListManagedUsersHandler(IUserAdministrationStore store)
    : IRequestHandler<ListManagedUsersQuery, Outcome<IReadOnlyList<ManagedUser>>>
{
    public Task<Outcome<IReadOnlyList<ManagedUser>>> Handle(
        ListManagedUsersQuery query, CancellationToken ct) =>
        store.ListAsync(query.Search, query.Page, ct);
}
public sealed class ListManagedRolesHandler(IUserAdministrationStore store)
    : IRequestHandler<ListManagedRolesQuery, IReadOnlyList<ManagedRole>>
{
    public Task<IReadOnlyList<ManagedRole>> Handle(ListManagedRolesQuery query,
        CancellationToken ct) => store.ListRolesAsync(ct);
}
public sealed class CreateManagedUserHandler(IUserAdministrationStore store)
    : IRequestHandler<CreateManagedUserCommand, Outcome<ManagedUser>>
{
    public Task<Outcome<ManagedUser>> Handle(CreateManagedUserCommand command,
        CancellationToken ct) => store.CreateAsync(command.ActorId, command.Input, ct);
}
public sealed class GrantManagedRoleHandler(IUserAdministrationStore store)
    : IRequestHandler<GrantManagedRoleCommand, Outcome<GrantReceipt>>
{
    public Task<Outcome<GrantReceipt>> Handle(GrantManagedRoleCommand command,
        CancellationToken ct) => store.GrantAsync(command.ActorId, command.UserId, command.Input, ct);
}
public sealed class DeactivateManagedUserHandler(IUserAdministrationStore store)
    : IRequestHandler<DeactivateManagedUserCommand, Outcome<bool>>
{
    public Task<Outcome<bool>> Handle(DeactivateManagedUserCommand command,
        CancellationToken ct) => store.DeactivateAsync(command.ActorId, command.UserId, ct);
}
