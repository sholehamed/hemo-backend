using Hemo.Application.Cqrs;

namespace Hemo.Application.Authentication;

/// <summary>All identity use cases cross the application boundary through typed CQRS messages.</summary>
public sealed class AuthenticateHandler(IAuthenticationStore store)
    : IRequestHandler<AuthenticateCommand, Outcome<SignInTicket>>
{
    public Task<Outcome<SignInTicket>> Handle(AuthenticateCommand cmd, CancellationToken ct)
    {
        var userName = cmd.UserName?.Trim() ?? "";
        if (userName.Length is < 1 or > 100 || string.IsNullOrEmpty(cmd.Password)
            || cmd.Password.Length > 512)
            return Task.FromResult(Outcome<SignInTicket>.Fail("unauthorized"));
        return store.AuthenticateAsync(userName, cmd.Password,
            cmd.RemoteIp is { Length: <= 45 } ? cmd.RemoteIp : null, ct);
    }
}

public sealed class GetSessionHandler(IAuthenticationStore store)
    : IRequestHandler<GetSessionQuery, Outcome<SessionView>>
{
    public Task<Outcome<SessionView>> Handle(GetSessionQuery query, CancellationToken ct) =>
        query.UserId > 0 ? store.GetSessionAsync(query.UserId, ct)
            : Task.FromResult(Outcome<SessionView>.Fail("unauthorized"));
}

public sealed class ValidateSessionHandler(IAuthenticationStore store)
    : IRequestHandler<ValidateSessionQuery, bool>
{
    public Task<bool> Handle(ValidateSessionQuery query, CancellationToken ct) =>
        query.UserId > 0 && query.SessionId > 0
            ? store.ValidateSessionAsync(query.UserId, query.SessionId, ct)
            : Task.FromResult(false);
}

public sealed class RevokeSessionHandler(IAuthenticationStore store)
    : IRequestHandler<RevokeSessionCommand, Outcome<bool>>
{
    public Task<Outcome<bool>> Handle(RevokeSessionCommand cmd, CancellationToken ct) =>
        cmd.UserId > 0 && cmd.SessionId > 0
            ? store.RevokeSessionAsync(cmd.UserId, cmd.SessionId, ct)
            : Task.FromResult(Outcome<bool>.Fail("unauthorized"));
}

public sealed class HasGlobalPermissionHandler(IAuthenticationStore store)
    : IRequestHandler<HasGlobalPermissionQuery, bool>
{
    public Task<bool> Handle(HasGlobalPermissionQuery query, CancellationToken ct) =>
        query.UserId > 0 && !string.IsNullOrWhiteSpace(query.Permission)
            ? store.HasGlobalPermissionAsync(query.UserId, query.Permission, ct)
            : Task.FromResult(false);
}

public sealed class BootstrapSystemAdministratorHandler(IAuthenticationStore store)
    : IRequestHandler<BootstrapSystemAdministratorCommand, Outcome<bool>>
{
    public Task<Outcome<bool>> Handle(BootstrapSystemAdministratorCommand cmd,
        CancellationToken ct)
    {
        var userName = cmd.UserName?.Trim() ?? "";
        var displayName = cmd.DisplayName?.Trim() ?? "";
        var password = cmd.Password ?? "";
        if (userName.Length is < 1 or > 100 || displayName.Length is < 1 or > 180 ||
            password.Length < 16 || password.Length > 512 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            return Task.FromResult(Outcome<bool>.Fail("bootstrap_invalid_configuration"));
        return store.BootstrapSystemAdminAsync(userName, displayName, password, ct);
    }
}
