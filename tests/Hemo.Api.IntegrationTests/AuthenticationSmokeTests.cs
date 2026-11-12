using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class AuthenticationSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthenticationSmokeTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Hemo",
                "Server=127.0.0.1,65530;Database=Hemo;User Id=unused;Password=unused;Connect Timeout=1;Encrypt=False");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Csrf_bootstrap_is_anonymous_and_returns_cookie()
    {
        using var result = await _client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
        Assert.Contains(result.Headers.GetValues("Set-Cookie"), x =>
            x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Anonymous_session_is_401_not_redirect()
    {
        using var result = await _client.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Null(result.Headers.Location);
    }

    [Fact]
    public async Task Anonymous_global_permission_probe_is_401()
    {
        using var result = await _client.GetAsync("/api/v1/system/access-check");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }

    [Fact]
    public async Task Login_without_csrf_is_rejected_before_database_query()
    {
        using var result = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { userName = "unknown", password = "invalid" });
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task Logout_without_session_is_401()
    {
        using var result = await _client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
    }
}
