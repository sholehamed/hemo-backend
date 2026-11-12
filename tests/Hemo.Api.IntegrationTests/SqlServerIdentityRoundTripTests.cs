using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hemo.Api.Security;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class SqlServerIdentityRoundTripTests
{
    [Fact]
    [Trait("Category", "SqlServer")]
    public async Task Full_database_baseline_bootstrap_and_bff_round_trip()
    {
        var connString = Environment.GetEnvironmentVariable("HEMO_CI_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connString))
            return; // Opt-in local integration test. CI SQL job always sets this.

        var parsed = new SqlConnectionStringBuilder(connString);
        if (!parsed.DataSource.StartsWith("127.0.0.1,14333", StringComparison.Ordinal)
            || parsed.InitialCatalog is not ("master" or "Hemo"))
            throw new InvalidOperationException(
                "Destructive setup is only permitted against ephemeral loopback SQL Server on port 14333.");

        var master = new SqlConnectionStringBuilder(connString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await connection.OpenAsync();
                break;
            }
            catch (SqlException) when (attempt < 44)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        var sql = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Hemo_Database_SQLServer_2022.sql"));
        var batches = Regex.Split(sql, @"(?im)^\s*GO\s*(?:--[^\r\n]*)?$");
        for (var i = 0; i < batches.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(batches[i])) continue;
            await using var command = connection.CreateCommand();
            command.CommandText = batches[i];
            command.CommandTimeout = 90;
            try { await command.ExecuteNonQueryAsync(); }
            catch (SqlException e)
            {
                throw new InvalidOperationException($"Baseline failed at SQL batch {i + 1}", e);
            }
        }

        var hemo = new SqlConnectionStringBuilder(connString) { InitialCatalog = "Hemo" };
        var options = new DbContextOptionsBuilder<HemoDbContext>()
            .UseSqlServer(hemo.ConnectionString).Options;
        await using (var db = new HemoDbContext(options))
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["HEMO_BOOTSTRAP_USERNAME"] = "integration_admin",
                    ["HEMO_BOOTSTRAP_DISPLAY_NAME"] = "Integration Test Administrator",
                    ["HEMO_BOOTSTRAP_PASSWORD"] = "Hemo-Integration-Ci_2026!"
                }).Build();

            await AdminBootstrap.RunAsync(db, new PasswordHasher<AccountUser>(), config);
            Assert.Equal(1, await db.UserRoles
                .Where(g => g.IsActive)
                .Join(db.Roles, g => g.RoleId, r => r.RoleId,
                    (g, r) => r.Code)
                .CountAsync(code => code == "SYSTEM_ADMIN"));
        }

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("ConnectionStrings:Hemo", hemo.ConnectionString));
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        using var readiness = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, readiness.StatusCode);

        using var csrf = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.NoContent, csrf.StatusCode);
        var rawCookie = csrf.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split(';')[0].Substring("XSRF-TOKEN=".Length);
        var token = Uri.UnescapeDataString(rawCookie);

        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                userName = "integration_admin", password = "Hemo-Integration-Ci_2026!"
            })
        };
        login.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", token);
        using var signedIn = await client.SendAsync(login);
        Assert.Equal(HttpStatusCode.NoContent, signedIn.StatusCode);

        using var session = await client.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.OK, session.StatusCode);
        var payload = await session.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("integration_admin", payload.GetProperty("userName").GetString());
        Assert.True(payload.GetProperty("grants").EnumerateArray()
            .Any(g => g.GetProperty("roleCode").GetString() == "SYSTEM_ADMIN"));

        using var allowed = await client.GetAsync("/api/v1/system/access-check");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        logout.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", token);
        using var signedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, signedOut.StatusCode);

        using var afterLogout = await client.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
