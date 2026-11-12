using System.Net;
using Xunit;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hemo.Api.IntegrationTests;

public sealed class InfrastructureSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public InfrastructureSmokeTests(WebApplicationFactory<Program> factory)
    {
        // Health/live and status MUST not require a working SQL Server.
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Hemo",
                "Server=127.0.0.1,65530;Database=Hemo;User Id=unused;Password=unused;Connect Timeout=1;Encrypt=False");
        }).CreateClient();
    }

    [Fact]
    public async Task Liveness_does_not_query_sql()
    {
        using var result = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Contains("Healthy", await result.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Status_contract_is_safe_and_contains_trace_id()
    {
        var result = await _client.GetFromJsonAsync<SystemStatusResponse>("/api/v1/system/status");
        Assert.NotNull(result);
        Assert.Equal("hemo-api", result.Service);
        Assert.Equal("running", result.Status);
        Assert.False(string.IsNullOrWhiteSpace(result.TraceId));
    }

    [Fact]
    public async Task Unknown_api_route_is_404()
    {
        using var result = await _client.GetAsync("/api/v1/system/not-a-route");
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
