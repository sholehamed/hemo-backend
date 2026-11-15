using Hemo.Application.Cqrs;

namespace Hemo.Application.Authentication;

public sealed record SessionGrant(string RoleCode, long? FacilityId, long? DepartmentId,
    IReadOnlyList<string> Permissions);
public sealed record SessionView(long UserId, string UserName, string DisplayName,
    IReadOnlyList<SessionGrant> Grants);
public sealed record SignInTicket(long UserId, string UserName, long SessionId);

public sealed record AuthenticateCommand(string UserName, string Password, string? RemoteIp)
    : ICommand<Outcome<SignInTicket>>;
public sealed record GetSessionQuery(long UserId) : IQuery<Outcome<SessionView>>;
public sealed record ValidateSessionQuery(long UserId, long SessionId) : IQuery<bool>;
public sealed record RevokeSessionCommand(long UserId, long SessionId)
    : ICommand<Outcome<bool>>;
public sealed record HasGlobalPermissionQuery(long UserId, string Permission)
    : IQuery<bool>;
public sealed record BootstrapSystemAdministratorCommand(string? UserName,
    string? DisplayName, string? Password) : ICommand<Outcome<bool>>;

public interface IAuthenticationStore
{
    Task<Outcome<SignInTicket>> AuthenticateAsync(string userName,
        string password, string? remoteIp, CancellationToken ct);
    Task<Outcome<SessionView>> GetSessionAsync(long userId, CancellationToken ct);
    Task<bool> ValidateSessionAsync(long userId, long sessionId, CancellationToken ct);
    Task<Outcome<bool>> RevokeSessionAsync(long userId, long sessionId, CancellationToken ct);
    Task<bool> HasGlobalPermissionAsync(long userId, string code, CancellationToken ct);
    Task<Outcome<bool>> BootstrapSystemAdminAsync(string userName, string displayName,
        string password, CancellationToken ct);
}
