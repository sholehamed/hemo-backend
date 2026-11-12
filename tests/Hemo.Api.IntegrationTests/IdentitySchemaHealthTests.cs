using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class IdentitySchemaHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public IdentitySchemaHealthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Hemo",
                "Server=127.0.0.1,65530;Database=Hemo;User Id=unused;Password=unused;Connect Timeout=1;Encrypt=False");
        }).CreateClient();
    }

    [Fact]
    public async Task Readiness_is_unhealthy_without_sql_database()
    {
        using var result = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
        var body = await result.Content.ReadAsStringAsync();
        Assert.Contains("Unhealthy", body);
        Assert.Contains("sec-schema", body);
    }
}
