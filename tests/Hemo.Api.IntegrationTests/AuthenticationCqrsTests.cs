using Hemo.Application.Authentication;
using Hemo.Application.Cqrs;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class AuthenticationCqrsTests
{
    [Theory]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData("username", null)]
    public async Task Malformed_login_fails_without_database_access(string username, string? password)
    {
        var store = new SpyStore();
        var handler = new AuthenticateHandler(store);
        var response = await handler.Handle(new AuthenticateCommand(
            username, password!, null), CancellationToken.None);
        Assert.False(response.IsSuccess);
        Assert.Equal("unauthorized", response.Error);
        Assert.Equal(0, store.AuthCalls);
    }

    [Fact]
    public async Task Unknown_principal_and_invalid_session_do_not_access_store()
    {
        var store = new SpyStore();
        Assert.False(await new ValidateSessionHandler(store).Handle(
            new ValidateSessionQuery(0, 10), CancellationToken.None));
        Assert.False(await new ValidateSessionHandler(store).Handle(
            new ValidateSessionQuery(10, 0), CancellationToken.None));
        Assert.Equal(0, store.ValidateCalls);
    }

    [Fact]
    public async Task Bootstrap_rejects_weak_credentials_before_sql()
    {
        var store = new SpyStore();
        var result = await new BootstrapSystemAdministratorHandler(store).Handle(
            new BootstrapSystemAdministratorCommand("root", "Administrator", "123"),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("bootstrap_invalid_configuration", result.Error);
        Assert.Equal(0, store.BootCalls);
    }

    private sealed class SpyStore : IAuthenticationStore
    {
        public int AuthCalls, ValidateCalls, BootCalls;
        public Task<Outcome<SignInTicket>> AuthenticateAsync(string name,
            string password, string? ip, CancellationToken ct)
        {
            AuthCalls++;
            return Task.FromResult(Outcome<SignInTicket>.Fail("unauthorized"));
        }
        public Task<Outcome<SessionView>> GetSessionAsync(long user, CancellationToken ct) =>
            Task.FromResult(Outcome<SessionView>.Fail("unauthorized"));
        public Task<bool> ValidateSessionAsync(long user, long session, CancellationToken ct)
        {
            ValidateCalls++;
            return Task.FromResult(false);
        }
        public Task<Outcome<bool>> RevokeSessionAsync(long user, long session,
            CancellationToken ct) => Task.FromResult(Outcome<bool>.Fail("unauthorized"));
        public Task<bool> HasGlobalPermissionAsync(long user, string permission,
            CancellationToken ct) => Task.FromResult(false);
        public Task<Outcome<bool>> BootstrapSystemAdminAsync(string user,
            string display, string password, CancellationToken ct)
        {
            BootCalls++;
            return Task.FromResult(Outcome<bool>.Ok(true));
        }
    }
}
