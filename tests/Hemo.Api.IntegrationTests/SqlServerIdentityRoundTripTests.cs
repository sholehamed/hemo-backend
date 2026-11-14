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

        // Physical contract: the complete baseline is installed before authentication.
        await using (var catalog = connection.CreateCommand())
        {
            catalog.CommandText = """
                SELECT
                    (SELECT COUNT(*) FROM sys.tables WHERE SCHEMA_NAME(schema_id) IN
                        ('org','sec','patient','clinical','blood','equip','hv',
                         'integration','report','cfg','audit')),
                    (SELECT COUNT(*) FROM sys.foreign_keys WHERE is_ms_shipped=0),
                    (SELECT COUNT(*) FROM sys.indexes i
                     JOIN sys.tables t ON i.object_id=t.object_id
                     WHERE i.type=2 AND t.is_ms_shipped=0);
                """;
            await using var reader = await catalog.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal(88, reader.GetInt32(0));
            Assert.Equal(203, reader.GetInt32(1));
            Assert.Equal(259, reader.GetInt32(2));
        }

        await using (var seedContract = connection.CreateCommand())
        {
            seedContract.CommandText = """
                SELECT (SELECT COUNT(*) FROM sec.Role),
                       (SELECT COUNT(*) FROM cfg.Setting
                        WHERE SettingKey='schema.version' AND SettingValue=N'1.0.0'
                          AND FacilityId IS NULL);
                """;
            await using var result = await seedContract.ExecuteReaderAsync();
            Assert.True(await result.ReadAsync());
            Assert.Equal(8, result.GetInt32(0));
            Assert.Equal(1, result.GetInt32(1));
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

        // ASP.NET Core antiforgery tokens are tied to the authenticated identity.
        // Refresh CSRF after login; the Angular client does this before logout.
        using var authenticatedCsrf = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.NoContent, authenticatedCsrf.StatusCode);
        var freshCookie = authenticatedCsrf.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split(';')[0].Substring("XSRF-TOKEN=".Length);
        var freshToken = Uri.UnescapeDataString(freshCookie);

        // ST-03: real SQL Server authorization across two facilities and departments.
        async Task<HttpResponseMessage> PostAdminAsync(string uri, object value)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = JsonContent.Create(value) };
            request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", freshToken);
            return await client.SendAsync(request);
        }

        async Task<long> CreateOrgAsync(string uri, object value)
        {
            using var response = await PostAdminAsync(uri, value);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var data = await response.Content.ReadFromJsonAsync<JsonElement>();
            return data.GetProperty("id").GetInt64();
        }

        var facilityA = await CreateOrgAsync("/api/v1/admin/organization/facilities",
            new { code = "ST03-HOSP-A", name = "Hospital A" });
        var facilityB = await CreateOrgAsync("/api/v1/admin/organization/facilities",
            new { code = "ST03-HOSP-B", name = "Hospital B" });
        var deptA1 = await CreateOrgAsync("/api/v1/admin/organization/departments",
            new { facilityId = facilityA, code = "ICU", name = "ICU A", parentDepartmentId = (long?)null });
        var deptA2 = await CreateOrgAsync("/api/v1/admin/organization/departments",
            new { facilityId = facilityA, code = "ER", name = "ER A", parentDepartmentId = (long?)null });
        var deptB1 = await CreateOrgAsync("/api/v1/admin/organization/departments",
            new { facilityId = facilityB, code = "ICU", name = "ICU B", parentDepartmentId = (long?)null });

        using var createdUser = await PostAdminAsync("/api/v1/admin/users/",
            new { userName = "scoped_admission", displayName = "Scoped Admission",
                  initialPassword = "Scoped-Ci_2026-Str0ng!" });
        Assert.Equal(HttpStatusCode.Created, createdUser.StatusCode);
        var userBody = await createdUser.Content.ReadFromJsonAsync<JsonElement>();
        var scopedUserId = userBody.GetProperty("id").GetInt64();

        long admissionRoleId;
        await using (var db = new HemoDbContext(options))
        {
            admissionRoleId = await db.Roles.Where(r => r.Code == "ADMISSION")
                .Select(r => r.RoleId).SingleAsync();
        }

        using var assignment = await PostAdminAsync(
            $"/api/v1/admin/users/{scopedUserId}/grants",
            new { roleId = admissionRoleId, facilityId = (long?)facilityA,
                  departmentId = (long?)deptA1 });
        Assert.Equal(HttpStatusCode.Created, assignment.StatusCode);

        // The backend validates each relationship instead of trusting frontend options.
        using var illegalGrant = await PostAdminAsync(
            $"/api/v1/admin/users/{scopedUserId}/grants",
            new { roleId = admissionRoleId, facilityId = (long?)facilityB,
                  departmentId = (long?)deptA1 });
        Assert.Equal(HttpStatusCode.BadRequest, illegalGrant.StatusCode);

        using var scopedClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        { AllowAutoRedirect = false, HandleCookies = true });
        using var newCsrf = await scopedClient.GetAsync("/api/v1/auth/csrf");
        var scopedToken = Uri.UnescapeDataString(newCsrf.Headers.GetValues("Set-Cookie")
            .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
            .Split(';')[0].Substring("XSRF-TOKEN=".Length));

        using var scopedLogin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                userName = "scoped_admission", password = "Scoped-Ci_2026-Str0ng!"
            })
        };
        scopedLogin.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", scopedToken);
        using var signedInScoped = await scopedClient.SendAsync(scopedLogin);
        Assert.Equal(HttpStatusCode.NoContent, signedInScoped.StatusCode);

        using var permitted = await scopedClient.GetAsync(
            $"/api/v1/organization/facilities/{facilityA}/access-check?departmentId={deptA1}");
        using var otherDept = await scopedClient.GetAsync(
            $"/api/v1/organization/facilities/{facilityA}/access-check?departmentId={deptA2}");
        using var otherHospital = await scopedClient.GetAsync(
            $"/api/v1/organization/facilities/{facilityB}/access-check?departmentId={deptB1}");
        Assert.Equal(HttpStatusCode.OK, permitted.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherDept.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherHospital.StatusCode);

        using var cannotManage = await scopedClient.GetAsync("/api/v1/admin/users/");
        Assert.Equal(HttpStatusCode.Forbidden, cannotManage.StatusCode);

        // ST-04 full SQL-backed admission vertical slice, including scope checks and audit.
        using var scopedCsrfAfterLogin = await scopedClient.GetAsync("/api/v1/auth/csrf");
        var scopedFreshToken = Uri.UnescapeDataString(
            scopedCsrfAfterLogin.Headers.GetValues("Set-Cookie")
                .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
                .Split(';')[0].Substring("XSRF-TOKEN=".Length));

        async Task<HttpResponseMessage> PostScopedAsync(string uri, object body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = JsonContent.Create(body) };
            request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", scopedFreshToken);
            return await scopedClient.SendAsync(request);
        }
        async Task<HttpResponseMessage> PutScopedAsync(string uri, object body)
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, uri)
            { Content = JsonContent.Create(body) };
            request.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", scopedFreshToken);
            return await scopedClient.SendAsync(request);
        }

        var baseAdmission = $"/api/v1/facilities/{facilityA}/departments/{deptA1}/admissions";
        var patientDraft = new
        {
            nationalId = "0012345678", isUnknown = false,
            firstName = "آرمان", lastName = "آزمایشی",
            fatherName = "کریم", birthDate = new DateOnly(1996, 6, 10),
            sex = "M", mobile = (string?)null,
            admissionType = "Routine", bedId = (long?)null,
            admissionNote = (string?)null
        };
        using var admitted = await PostScopedAsync(baseAdmission + "/", patientDraft);
        Assert.Equal(HttpStatusCode.Created, admitted.StatusCode);
        var admission = await admitted.Content.ReadFromJsonAsync<JsonElement>();
        var encounterId = admission.GetProperty("encounterId").GetInt64();
        var originalRowVersion = admission.GetProperty("patientRowVersion").GetString();
        Assert.False(string.IsNullOrWhiteSpace(originalRowVersion));

        using var persisted = await scopedClient.GetAsync($"{baseAdmission}/{encounterId}");
        Assert.Equal(HttpStatusCode.OK, persisted.StatusCode);
        var persistedAdmission = await persisted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("آرمان", persistedAdmission.GetProperty("firstName").GetString());

        using var list = await scopedClient.GetAsync(baseAdmission + "/?page=1&search=آرمان");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var found = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(found.GetProperty("total").GetInt32() >= 1);

        using var blockedList = await scopedClient.GetAsync(
            $"/api/v1/facilities/{facilityA}/departments/{deptA2}/admissions/");
        Assert.Equal(HttpStatusCode.Forbidden, blockedList.StatusCode);
        using var blockedHospital = await scopedClient.GetAsync(
            $"/api/v1/facilities/{facilityB}/departments/{deptB1}/admissions/{encounterId}");
        Assert.Equal(HttpStatusCode.Forbidden, blockedHospital.StatusCode);

        using var issued = await PostScopedAsync($"{baseAdmission}/{encounterId}/wristbands", new { });
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var band = await issued.Content.ReadFromJsonAsync<JsonElement>();
        var barcode = band.GetProperty("barcode").GetString();
        Assert.False(string.IsNullOrWhiteSpace(barcode));
        using var issueAgain = await PostScopedAsync(
            $"{baseAdmission}/{encounterId}/wristbands", new { });
        Assert.Equal(HttpStatusCode.OK, issueAgain.StatusCode);
        var secondBand = await issueAgain.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(barcode, secondBand.GetProperty("barcode").GetString());

        using var mismatch = await PostScopedAsync(
            $"{baseAdmission}/{encounterId}/identity-verifications",
            new { barcode, firstIdentifier = "0000000000",
                secondIdentifier = "آرمان آزمایشی", checklistConfirmed = true,
                mismatchReason = "Incorrect identifier" });
        Assert.Equal(HttpStatusCode.OK, mismatch.StatusCode);
        var mismatchValue = await mismatch.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Mismatch", mismatchValue.GetProperty("status").GetString());

        using var verified = await PostScopedAsync(
            $"{baseAdmission}/{encounterId}/identity-verifications",
            new { barcode, firstIdentifier = "0012345678",
                secondIdentifier = "آرمان آزمایشی", checklistConfirmed = true,
                mismatchReason = (string?)null });
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        var verifiedValue = await verified.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Verified", verifiedValue.GetProperty("status").GetString());

        var updateDraft = new
        {
            firstName = "آرمان", lastName = "ویرایش‌شده",
            fatherName = "کریم", birthDate = new DateOnly(1996, 6, 10),
            sex = "M", mobile = (string?)null,
            rowVersion = originalRowVersion
        };
        using var updated = await PutScopedAsync(
            $"{baseAdmission}/{encounterId}/patient", updateDraft);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var staleUpdate = await PutScopedAsync(
            $"{baseAdmission}/{encounterId}/patient", updateDraft);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);

        using var duplicateIdentifier = await PostScopedAsync(baseAdmission + "/", patientDraft);
        Assert.Equal(HttpStatusCode.Conflict, duplicateIdentifier.StatusCode);

        var bedId = await CreateOrgAsync("/api/v1/admin/organization/beds",
            new { departmentId = deptA1, bedCode = "B-001" });
        using var assignedBed = await PostScopedAsync(baseAdmission + "/", new
        {
            nationalId = "0012345679", isUnknown = false,
            firstName = "سارا", lastName = "آزمایشی",
            fatherName = (string?)null, birthDate = (DateOnly?)null,
            sex = "F", mobile = (string?)null, admissionType = "Routine",
            bedId = (long?)bedId, admissionNote = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, assignedBed.StatusCode);

        using var occupiedBed = await PostScopedAsync(baseAdmission + "/", new
        {
            nationalId = "0012345670", isUnknown = false,
            firstName = "حسام", lastName = "آزمایشی",
            fatherName = (string?)null, birthDate = (DateOnly?)null,
            sex = "M", mobile = (string?)null, admissionType = "Routine",
            bedId = (long?)bedId, admissionNote = (string?)null
        });
        Assert.Equal(HttpStatusCode.Conflict, occupiedBed.StatusCode);

        await using (var patientDb = new HemoDbContext(options))
        {
            Assert.Equal(2, await patientDb.Encounters.CountAsync(x => x.FacilityId == facilityA));
            Assert.Equal(2, await patientDb.IdentityVerifications.CountAsync(
                x => x.EncounterId == encounterId));
            Assert.Equal(1, await patientDb.Wristbands.CountAsync(x => x.EncounterId == encounterId));
            Assert.True(await patientDb.AuditEvents.AnyAsync(x =>
                x.EntityType == "Encounter" && x.ActionType == "Admitted"));
        }

        using var disabled = await PostAdminAsync(
            $"/api/v1/admin/users/{scopedUserId}/deactivate", new { });
        Assert.Equal(HttpStatusCode.NoContent, disabled.StatusCode);
        using var revoked = await scopedClient.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);

        await using (var auditDb = new HemoDbContext(options))
        {
            Assert.True(await auditDb.AuditEvents.AnyAsync(
                x => x.EntityType == "UserRole" && x.ActionType == "Assigned"));
            Assert.True(await auditDb.UserSessions.AnyAsync(
                x => x.AccountUserId == scopedUserId && x.RevokedAt != null));
        }

        using var logout = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout")
        {
            Content = JsonContent.Create(new { })
        };
        logout.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", freshToken);
        using var signedOut = await client.SendAsync(logout);
        Assert.Equal(HttpStatusCode.NoContent, signedOut.StatusCode);

        using var afterLogout = await client.GetAsync("/api/v1/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
