using Hemo.Domain.Organization;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
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

            await AdminBootstrap.RunAsync(
                new Hemo.Application.Authentication.BootstrapSystemAdministratorHandler(
                    new Hemo.Infrastructure.Security.AuthenticationSqlStore(
                        db, new PasswordHasher<AccountUser>(),
                        new Hemo.Infrastructure.Security.HemoAccessService(db))), config);
            Assert.Equal(1, await db.UserRoles
                .Where(g => g.IsActive)
                .Join(db.Roles, g => g.RoleId, r => r.RoleId,
                    (g, r) => r.Code)
                .CountAsync(code => code == "SYSTEM_ADMIN"));
        }

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("ConnectionStrings:Hemo", hemo.ConnectionString)
                    .UseSetting("Security:LoginPermitLimit", "24"));
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

        // ST-05: a nurse submits a verified Encounter request; a separate doctor
        // approves it. Server scope, self-approval and concurrency are checked.
        async Task<(long UserId, HttpClient Http, string Csrf)> NewClinicianAsync(
            string username, string password, string roleCode, bool facilityWide = false)
        {
            using var userResponse = await PostAdminAsync("/api/v1/admin/users/", new
            {
                userName = username, displayName = "Clinical Fixture " + username,
                initialPassword = password
            });
            Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
            var userJson = await userResponse.Content.ReadFromJsonAsync<JsonElement>();
            var id = userJson.GetProperty("id").GetInt64();
            long roleId;
            await using (var roleDb = new HemoDbContext(options))
                roleId = await roleDb.Roles.Where(x => x.Code == roleCode)
                    .Select(x => x.RoleId).SingleAsync();
            using var granted = await PostAdminAsync(
                $"/api/v1/admin/users/{id}/grants", new
                {
                    roleId, facilityId = (long?)facilityA,
                    departmentId = facilityWide ? (long?)null : deptA1
                });
            Assert.Equal(HttpStatusCode.Created, granted.StatusCode);
            var http = factory.CreateClient(new WebApplicationFactoryClientOptions
            { AllowAutoRedirect = false, HandleCookies = true });
            using var initial = await http.GetAsync("/api/v1/auth/csrf");
            var initialCsrf = Uri.UnescapeDataString(initial.Headers.GetValues("Set-Cookie")
                .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
                .Split(';')[0].Substring("XSRF-TOKEN=".Length));
            using var signin = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
            {
                Content = JsonContent.Create(new { userName = username, password })
            };
            signin.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", initialCsrf);
            using var response = await http.SendAsync(signin);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            using var fresh = await http.GetAsync("/api/v1/auth/csrf");
            var csrfToken = Uri.UnescapeDataString(fresh.Headers.GetValues("Set-Cookie")
                .First(x => x.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal))
                .Split(';')[0].Substring("XSRF-TOKEN=".Length));
            return (id, http, csrfToken);
        }

        async Task<HttpResponseMessage> ClinicalPostAsync(HttpClient http,
            string uri, string csrfValue, object input)
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = JsonContent.Create(input) };
            message.Headers.TryAddWithoutValidation("X-XSRF-TOKEN", csrfValue);
            return await http.SendAsync(message);
        }

        var nurse = await NewClinicianAsync("st05_nurse", "Nurse-Ci_2026-Str0ng!", "NURSE");
        var doctor = await NewClinicianAsync("st05_doctor", "Doctor-Ci_2026-Str0ng!", "DOCTOR");
        using var nurseHttp = nurse.Http;
        using var doctorHttp = doctor.Http;
        var requestRoot = $"/api/v1/facilities/{facilityA}/departments/{deptA1}/blood-requests";
        using var productResponse = await nurseHttp.GetAsync(requestRoot + "/products");
        Assert.Equal(HttpStatusCode.OK, productResponse.StatusCode);
        var productsJson = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rbc = productsJson.EnumerateArray()
            .First(x => x.GetProperty("code").GetString() == "RBC");
        var productId = rbc.GetProperty("id").GetInt64();
        var draft = new
        {
            encounterId, productTypeId = productId, quantity = 1m,
            priority = "Routine", indication = "Synthetic integration verification only",
            diagnosis = (string?)null, specialRequirements = (string?)null,
            hemoglobin = (decimal?)null, previousReaction = (bool?)null
        };
        using var createdRequest = await ClinicalPostAsync(
            nurseHttp, requestRoot + "/", nurse.Csrf, draft);
        Assert.Equal(HttpStatusCode.Created, createdRequest.StatusCode);
        var requestJson = await createdRequest.Content.ReadFromJsonAsync<JsonElement>();
        var bloodRequestId = requestJson.GetProperty("bloodRequestId").GetInt64();
        var version = requestJson.GetProperty("rowVersion").GetString();
        Assert.Equal("AwaitingDoctor", requestJson.GetProperty("status").GetString());

        using var scopedRequestDenied = await nurseHttp.GetAsync(
            $"/api/v1/facilities/{facilityB}/departments/{deptB1}/blood-requests/{bloodRequestId}");
        Assert.Equal(HttpStatusCode.Forbidden, scopedRequestDenied.StatusCode);
        using var nurseReview = await ClinicalPostAsync(
            nurseHttp, $"{requestRoot}/{bloodRequestId}/review", nurse.Csrf,
            new { decision = "Approved", reason = (string?)null, rowVersion = version });
        Assert.Equal(HttpStatusCode.Forbidden, nurseReview.StatusCode);

        using var doctorReview = await ClinicalPostAsync(
            doctorHttp, $"{requestRoot}/{bloodRequestId}/review", doctor.Csrf,
            new { decision = "Approved", reason = (string?)null, rowVersion = version });
        Assert.Equal(HttpStatusCode.OK, doctorReview.StatusCode);
        var reviewedJson = await doctorReview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Approved", reviewedJson.GetProperty("status").GetString());

        using var staleReview = await ClinicalPostAsync(
            doctorHttp, $"{requestRoot}/{bloodRequestId}/review", doctor.Csrf,
            new { decision = "Rejected", reason = "Concurrent review",
                rowVersion = version });
        Assert.Equal(HttpStatusCode.Conflict, staleReview.StatusCode);
        await using (var clinicalDb = new HemoDbContext(options))
        {
            Assert.Equal(1, await clinicalDb.RequestApprovals.CountAsync(
                x => x.BloodRequestId == bloodRequestId));
            Assert.True(await clinicalDb.ClinicalTasks.AnyAsync(x =>
                x.BloodRequestId == bloodRequestId && x.Status == "Completed"));
            Assert.True(await clinicalDb.AuditEvents.AnyAsync(x =>
                x.EntityType == "BloodRequest" && x.EntityId == bloodRequestId
                && x.ActionType == "Approved"));
        }

        // ST-06: product-matched unit reservation and independently reviewed lab evidence.
        var expert = await NewClinicianAsync("st06_bank", "Bank-Ci_2026-Str0ng!",
            "BLOOD_TRANSFUSION_EXPERT", true);
        var supervisor = await NewClinicianAsync("st06_super", "Super-Ci_2026-Str0ng!",
            "SUPERVISOR", true);
        using var bankHttp = expert.Http;
        using var supervisorHttp = supervisor.Http;
        var bankRoot = $"/api/v1/facilities/{facilityA}/blood-bank";
        using var bloodGroupsResponse = await bankHttp.GetAsync(bankRoot + "/blood-groups");
        Assert.Equal(HttpStatusCode.OK, bloodGroupsResponse.StatusCode);
        var groups = await bloodGroupsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bloodGroupId = groups.EnumerateArray()
            .First(x => x.GetProperty("code").GetString() == "O+")
            .GetProperty("id").GetInt64();
        using var stockReceived = await ClinicalPostAsync(bankHttp, bankRoot + "/units",
            expert.Csrf, new { unitCode = "ST06-BLOOD-001", productTypeId = productId,
                bloodGroupId, expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(28)),
                storageLocation = "CI / synthetic" });
        Assert.Equal(HttpStatusCode.Created, stockReceived.StatusCode);
        var unitJson = await stockReceived.Content.ReadFromJsonAsync<JsonElement>();
        var stockUnitId = unitJson.GetProperty("id").GetInt64();
        using var secondReceipt = await ClinicalPostAsync(bankHttp, bankRoot + "/units",
            expert.Csrf, new { unitCode = "ST06-BLOOD-001", productTypeId = productId,
                bloodGroupId, expiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(28)),
                storageLocation = "CI / synthetic" });
        Assert.Equal(HttpStatusCode.Conflict, secondReceipt.StatusCode);

        using var blockedBank = await nurseHttp.GetAsync(bankRoot + "/units");
        Assert.Equal(HttpStatusCode.Forbidden, blockedBank.StatusCode);
        using var wrongFacility = await bankHttp.GetAsync(
            $"/api/v1/facilities/{facilityB}/blood-bank/units");
        Assert.Equal(HttpStatusCode.Forbidden, wrongFacility.StatusCode);

        using var reservationResponse = await ClinicalPostAsync(bankHttp,
            bankRoot + "/reservations", expert.Csrf,
            new { unitId = stockUnitId, requestId = bloodRequestId });
        Assert.Equal(HttpStatusCode.OK, reservationResponse.StatusCode);
        var reservationId = await reservationResponse.Content.ReadFromJsonAsync<long>();
        Assert.True(reservationId > 0);
        using var doubleReserve = await ClinicalPostAsync(bankHttp,
            bankRoot + "/reservations", expert.Csrf,
            new { unitId = stockUnitId, requestId = bloodRequestId });
        Assert.Equal(HttpStatusCode.Conflict, doubleReserve.StatusCode);

        using var performed = await ClinicalPostAsync(bankHttp, bankRoot + "/crossmatches",
            expert.Csrf, new { reservationId, compatibilityResult = "Compatible",
                testCode = "XM-CI", testResult = "Recorded", comments = "Synthetic lab result" });
        Assert.Equal(HttpStatusCode.OK, performed.StatusCode);
        var crossJson = await performed.Content.ReadFromJsonAsync<JsonElement>();
        var crossmatchId = crossJson.GetProperty("id").GetInt64();
        var crossVersion = crossJson.GetProperty("rowVersion").GetString();
        Assert.Equal("PendingReview", crossJson.GetProperty("status").GetString());
        using var finalBySupervisor = await ClinicalPostAsync(supervisorHttp,
            $"{bankRoot}/crossmatches/{crossmatchId}/finalize", supervisor.Csrf,
            new { rowVersion = crossVersion });
        Assert.Equal(HttpStatusCode.OK, finalBySupervisor.StatusCode);
        var finalized = await finalBySupervisor.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Final", finalized.GetProperty("status").GetString());
        using var duplicateFinal = await ClinicalPostAsync(supervisorHttp,
            $"{bankRoot}/crossmatches/{crossmatchId}/finalize", supervisor.Csrf,
            new { rowVersion = crossVersion });
        Assert.Equal(HttpStatusCode.Conflict, duplicateFinal.StatusCode);
        await using (var bankDb = new HemoDbContext(options))
        {
            Assert.Equal(1, await bankDb.UnitReservations.CountAsync(x =>
                x.BloodUnitId == stockUnitId && x.Status == "Active"));
            Assert.Equal(1, await bankDb.CrossmatchTests.CountAsync(x =>
                x.CrossmatchId == crossmatchId));
            Assert.True(await bankDb.AuditEvents.AnyAsync(x =>
                x.EntityType == "Crossmatch" && x.EntityId == crossmatchId
                && x.ActionType == "Finalized"));
        }

        // ST-07: handoff requires a finalized compatible lab record, ward receipt
        // by a distinct authorized nurse, and a reviewed quarantine return.
        var logisticsBank = $"/api/v1/facilities/{facilityA}/logistics";
        var wardLogistics = $"/api/v1/facilities/{facilityA}/departments/{deptA1}/logistics";
        using var issuedUnit = await ClinicalPostAsync(bankHttp, logisticsBank + "/issues",
            expert.Csrf, new { unitId = stockUnitId, requestId = bloodRequestId,
                departmentId = deptA1, conditionNote = "Synthetic issue verification" });
        Assert.Equal(HttpStatusCode.OK, issuedUnit.StatusCode);
        var issuedJson = await issuedUnit.Content.ReadFromJsonAsync<JsonElement>();
        var issueId = issuedJson.GetProperty("id").GetInt64();
        using var duplicateLogisticsIssue = await ClinicalPostAsync(bankHttp, logisticsBank + "/issues",
            expert.Csrf, new { unitId = stockUnitId, requestId = bloodRequestId,
                departmentId = deptA1, conditionNote = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, duplicateLogisticsIssue.StatusCode);

        using var handedOff = await ClinicalPostAsync(supervisorHttp,
            logisticsBank + "/dispatches", supervisor.Csrf,
            new { issueId, containerType = "CI sealed box",
                temperatureAtExitC = (decimal?)4.5m, note = "Synthetic dispatch" });
        Assert.Equal(HttpStatusCode.OK, handedOff.StatusCode);
        var dispatchJson = await handedOff.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InTransit", dispatchJson.GetProperty("status").GetString());

        using var unauthorizedWard = await nurseHttp.GetAsync(
            $"/api/v1/facilities/{facilityB}/departments/{deptB1}/logistics/issues");
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedWard.StatusCode);
        using var wardIssues = await nurseHttp.GetAsync(wardLogistics + "/issues");
        Assert.Equal(HttpStatusCode.OK, wardIssues.StatusCode);
        var issuesJson = await wardIssues.Content.ReadFromJsonAsync<JsonElement>();
        var inTransit = issuesJson.EnumerateArray()
            .Single(x => x.GetProperty("id").GetInt64() == issueId);
        var issueVersion = inTransit.GetProperty("rowVersion").GetString();

        using var wardReceipt = await ClinicalPostAsync(nurseHttp,
            $"{wardLogistics}/issues/{issueId}/receive", nurse.Csrf,
            new { rowVersion = issueVersion, conditionNote = "Unit physically received in synthetic ward" });
        Assert.Equal(HttpStatusCode.OK, wardReceipt.StatusCode);
        var receiptJson = await wardReceipt.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Received", receiptJson.GetProperty("status").GetString());
        using var duplicateReceipt = await ClinicalPostAsync(nurseHttp,
            $"{wardLogistics}/issues/{issueId}/receive", nurse.Csrf,
            new { rowVersion = issueVersion, conditionNote = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, duplicateReceipt.StatusCode);

        using var returnedUnit = await ClinicalPostAsync(supervisorHttp,
            $"{wardLogistics}/issues/{issueId}/return", supervisor.Csrf,
            new { returnedByUserId = nurse.UserId, reasonCode = "CI-UNUSED",
                conditionCode = "Sealed", temperatureC = (decimal?)4.0m,
                notes = "Quarantine until independent signoff" });
        Assert.Equal(HttpStatusCode.OK, returnedUnit.StatusCode);
        var returnJson = await returnedUnit.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", returnJson.GetProperty("status").GetString());
        var returnId = returnJson.GetProperty("id").GetInt64();
        var returnVersion = returnJson.GetProperty("rowVersion").GetString();

        using var selfReview = await ClinicalPostAsync(supervisorHttp,
            $"{logisticsBank}/returns/{returnId}/review", supervisor.Csrf,
            new { decision = "Quarantine", rowVersion = returnVersion,
                note = (string?)null });
        Assert.Equal(HttpStatusCode.Forbidden, selfReview.StatusCode);

        var supervisor2 = await NewClinicianAsync("st07_review",
            "Super2-Ci_2026-Str0ng!", "SUPERVISOR", true);
        using var supervisor2Http = supervisor2.Http;
        using var reviewedReturn = await ClinicalPostAsync(supervisor2Http,
            $"{logisticsBank}/returns/{returnId}/review", supervisor2.Csrf,
            new { decision = "Quarantine", rowVersion = returnVersion,
                note = "Keep segregated pending hospital SOP" });
        Assert.Equal(HttpStatusCode.OK, reviewedReturn.StatusCode);
        await using (var logisticsDb = new HemoDbContext(options))
        {
            Assert.True(await logisticsDb.UnitIssues.AnyAsync(x =>
                x.UnitIssueId == issueId && x.Status == "Returned"));
            Assert.True(await logisticsDb.Dispatches.AnyAsync(x =>
                x.DispatchId == dispatchJson.GetProperty("id").GetInt64()
                && x.Status == "Delivered"));
            Assert.True(await logisticsDb.BloodUnits.AnyAsync(x =>
                x.BloodUnitId == stockUnitId && x.IsQuarantined
                && x.Status == "Quarantined"));
            Assert.True(await logisticsDb.UnitReturns.AnyAsync(x =>
                x.UnitReturnId == returnId && x.Status == "Approved"
                && x.Decision == "Quarantine"));
        }

        // ST-08: independent fresh request and unit, so the ST-07 returned unit
        // remains quarantined and cannot be repurposed for a transfusion.
        using var st08RequestResponse = await ClinicalPostAsync(
            nurseHttp,requestRoot+"/",nurse.Csrf,draft);
        Assert.Equal(HttpStatusCode.Created,st08RequestResponse.StatusCode);
        var st08RequestJson = await st08RequestResponse.Content.ReadFromJsonAsync<JsonElement>();
        var st08RequestId=st08RequestJson.GetProperty("bloodRequestId").GetInt64();
        using var st08Approved = await ClinicalPostAsync(doctorHttp,
            $"{requestRoot}/{st08RequestId}/review",doctor.Csrf,
            new {decision="Approved",reason=(string?)null,
                rowVersion=st08RequestJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.OK,st08Approved.StatusCode);

        using var st08Receipt=await ClinicalPostAsync(bankHttp,bankRoot+"/units",
            expert.Csrf,new {unitCode="ST08-BLOOD-002",productTypeId=productId,
                bloodGroupId,expiryDate=DateOnly.FromDateTime(DateTime.UtcNow.AddDays(29)),
                storageLocation="CI / synthetic"});
        Assert.Equal(HttpStatusCode.Created,st08Receipt.StatusCode);
        var st08Unit=(await st08Receipt.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("id").GetInt64();
        using var st08Res=await ClinicalPostAsync(bankHttp,bankRoot+"/reservations",
            expert.Csrf,new {unitId=st08Unit,requestId=st08RequestId});
        Assert.Equal(HttpStatusCode.OK,st08Res.StatusCode);
        var st08Reservation=await st08Res.Content.ReadFromJsonAsync<long>();
        using var st08Lab=await ClinicalPostAsync(bankHttp,bankRoot+"/crossmatches",
            expert.Csrf,new {reservationId=st08Reservation,compatibilityResult="Compatible",
                testCode="XM-CI-08",testResult="Documented",
                comments="Approved laboratory evidence fixture"});
        Assert.Equal(HttpStatusCode.OK,st08Lab.StatusCode);
        var st08LabJson=await st08Lab.Content.ReadFromJsonAsync<JsonElement>();
        using var st08LabReview=await ClinicalPostAsync(supervisorHttp,
            $"{bankRoot}/crossmatches/{st08LabJson.GetProperty("id").GetInt64()}/finalize",
            supervisor.Csrf,new {rowVersion=st08LabJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.OK,st08LabReview.StatusCode);
        using var st08Issued=await ClinicalPostAsync(bankHttp,logisticsBank+"/issues",
            expert.Csrf,new {unitId=st08Unit,requestId=st08RequestId,
                departmentId=deptA1,conditionNote="Fixture issue"});
        Assert.Equal(HttpStatusCode.OK,st08Issued.StatusCode);
        var st08IssueJson=await st08Issued.Content.ReadFromJsonAsync<JsonElement>();
        var st08IssueId=st08IssueJson.GetProperty("id").GetInt64();
        using var st08Received=await ClinicalPostAsync(nurseHttp,
            $"{wardLogistics}/issues/{st08IssueId}/receive",nurse.Csrf,
            new {rowVersion=st08IssueJson.GetProperty("rowVersion").GetString(),
                conditionNote="Bedside confirmed"});
        Assert.Equal(HttpStatusCode.OK,st08Received.StatusCode);

        var transfusionRoot=
            $"/api/v1/facilities/{facilityA}/departments/{deptA1}/transfusions";
        using var wrongScopeTransfusion=await nurseHttp.GetAsync(
            $"/api/v1/facilities/{facilityB}/departments/{deptB1}/transfusions/");
        Assert.Equal(HttpStatusCode.Forbidden,wrongScopeTransfusion.StatusCode);
        using var prepared=await ClinicalPostAsync(nurseHttp,transfusionRoot+"/",
            nurse.Csrf,new {issueId=st08IssueId});
        Assert.Equal(HttpStatusCode.OK,prepared.StatusCode);
        var preparedJson=await prepared.Content.ReadFromJsonAsync<JsonElement>();
        var transfusionId=preparedJson.GetProperty("id").GetInt64();
        var prepVersion=preparedJson.GetProperty("rowVersion").GetString();
        Assert.Equal("Planned",preparedJson.GetProperty("status").GetString());
        using var noBaselineStart=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/start",nurse.Csrf,
            new {rowVersion=prepVersion,reason=(string?)null});
        Assert.Equal(HttpStatusCode.Conflict,noBaselineStart.StatusCode);

        using var baseline=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/vitals",nurse.Csrf,new {
                timepointCode="Baseline",observedAt=DateTime.UtcNow,
                systolicBp=(short?)120,diastolicBp=(short?)80,heartRate=(short?)78,
                temperatureC=(decimal?)36.7m,spo2=(byte?)99,respiratoryRate=(short?)18,
                urineOutputMl=(decimal?)null,alertFlag=false,
                clinicalNote=(string?)"Synthetic baseline"});
        Assert.Equal(HttpStatusCode.OK,baseline.StatusCode);
        using var checklist=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/checklist",nurse.Csrf,new {
                patientIdentityMatched=true,unitIdentityMatched=true,bloodGroupChecked=true,
                compatibilityChecked=true,expiryChecked=true,baselineVitalChecked=true,
                note="Physically verified by clinical fixture"});
        Assert.Equal(HttpStatusCode.OK,checklist.StatusCode);
        using var started=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/start",nurse.Csrf,
            new {rowVersion=prepVersion,reason=(string?)null});
        Assert.Equal(HttpStatusCode.OK,started.StatusCode);
        var startedJson=await started.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("InProgress",startedJson.GetProperty("status").GetString());
        using var staleStart=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/start",nurse.Csrf,
            new {rowVersion=prepVersion,reason=(string?)null});
        Assert.Equal(HttpStatusCode.Conflict,staleStart.StatusCode);

        using var during=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/vitals",nurse.Csrf,new {
                timepointCode="During",observedAt=DateTime.UtcNow,
                systolicBp=(short?)115,diastolicBp=(short?)75,heartRate=(short?)82,
                temperatureC=(decimal?)37.0m,spo2=(byte?)98,respiratoryRate=(short?)19,
                urineOutputMl=(decimal?)null,alertFlag=false,
                clinicalNote=(string?)"Synthetic monitoring"});
        Assert.Equal(HttpStatusCode.OK,during.StatusCode);
        using var completed=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/complete",nurse.Csrf,
            new {rowVersion=startedJson.GetProperty("rowVersion").GetString(),
                reason=(string?)null});
        Assert.Equal(HttpStatusCode.OK,completed.StatusCode);
        var completedJson=await completed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Completed",completedJson.GetProperty("status").GetString());

        using var selfSignoff=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/signoff",nurse.Csrf,
            new {rowVersion=completedJson.GetProperty("rowVersion").GetString(),
                comment=(string?)null});
        Assert.Equal(HttpStatusCode.Forbidden,selfSignoff.StatusCode);
        var secondNurse=await NewClinicianAsync("st08_independent",
            "Nurse2-Ci_2026-Str0ng!","NURSE");
        using var secondNurseHttp=secondNurse.Http;
        using var independent=await ClinicalPostAsync(secondNurseHttp,
            $"{transfusionRoot}/{transfusionId}/signoff",secondNurse.Csrf,
            new {rowVersion=completedJson.GetProperty("rowVersion").GetString(),
                comment="Independent synthetic verification"});
        Assert.Equal(HttpStatusCode.OK,independent.StatusCode);
        using var afterSignoff=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/vitals",nurse.Csrf,new {
                timepointCode="FollowUp",observedAt=DateTime.UtcNow,
                systolicBp=(short?)120,diastolicBp=(short?)80,heartRate=(short?)78,
                temperatureC=(decimal?)36.9m,spo2=(byte?)99,respiratoryRate=(short?)18,
                urineOutputMl=(decimal?)null,alertFlag=false,clinicalNote=(string?)null});
        Assert.Equal(HttpStatusCode.Conflict,afterSignoff.StatusCode);
        using var secondPreparation=await ClinicalPostAsync(nurseHttp,
            transfusionRoot+"/",nurse.Csrf,new {issueId=st08IssueId});
        Assert.Equal(HttpStatusCode.Conflict,secondPreparation.StatusCode);
        using var unsafeReturn=await ClinicalPostAsync(supervisorHttp,
            $"{wardLogistics}/issues/{st08IssueId}/return",supervisor.Csrf,
            new {returnedByUserId=nurse.UserId,reasonCode="INAPPROPRIATE",
                conditionCode="Sealed",temperatureC=(decimal?)4m,
                notes="Must be denied after transfusion"});
        Assert.Equal(HttpStatusCode.Conflict,unsafeReturn.StatusCode);
        using var invalidCompletedStop=await ClinicalPostAsync(nurseHttp,
            $"{transfusionRoot}/{transfusionId}/stop",nurse.Csrf,
            new {rowVersion=completedJson.GetProperty("rowVersion").GetString(),
                reason="Cannot stop a completed and signed off transfusion"});
        Assert.Equal(HttpStatusCode.Conflict,invalidCompletedStop.StatusCode);

        await using(var st08db=new HemoDbContext(options)) {
            Assert.Equal(2,await st08db.VitalObservations.CountAsync(v=>
                v.TransfusionId==transfusionId));
            Assert.Equal(1,await st08db.TransfusionSignoffs.CountAsync(x=>
                x.TransfusionId==transfusionId));
            Assert.True(await st08db.TransfusionActions.AnyAsync(x=>
                x.TransfusionId==transfusionId && x.ActionType=="Complete"));
        }

        // ST-09: delayed reaction on a real completed/signed ST-08 transfusion.
        // This test does not simulate stopping a physical bedside infusion.
        var reactionRoot=$"/api/v1/facilities/{facilityA}/departments/{deptA1}/reactions";
        using var outOfScopeReaction=await nurseHttp.GetAsync(
            $"/api/v1/facilities/{facilityB}/departments/{deptB1}/reactions/");
        Assert.Equal(HttpStatusCode.Forbidden,outOfScopeReaction.StatusCode);
        using var reactionReported=await ClinicalPostAsync(nurseHttp,reactionRoot+"/",
            nurse.Csrf,new {transfusionId,detectedAt=DateTime.UtcNow,
                transfusionStopped=false,symptomsSummary="Synthetic urticaria after completion",
                immediateActions="Escalated to assigned physician; documented assessment",
                narrative="This event contains no real patient data",
                symptomCodes=new[]{"RASH","FEVER"}});
        Assert.Equal(HttpStatusCode.OK,reactionReported.StatusCode);
        var reactionJson=await reactionReported.Content.ReadFromJsonAsync<JsonElement>();
        var eventId=reactionJson.GetProperty("id").GetInt64();
        Assert.Equal("Reported",reactionJson.GetProperty("status").GetString());
        using var deniedAssessment=await ClinicalPostAsync(nurseHttp,
            $"{reactionRoot}/{eventId}/assess",nurse.Csrf,new {
                diagnosisText="Not authorized",severityGrade=(byte?)null,
                causality=(string?)null,interventions=(string?)null,
                recommendations=(string?)null,outcome=(string?)null,
                nationalReportRequired=false,
                rowVersion=reactionJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.Forbidden,deniedAssessment.StatusCode);

        using var doctorAssessed=await ClinicalPostAsync(doctorHttp,
            $"{reactionRoot}/{eventId}/assess",doctor.Csrf,new {
                diagnosisText="Clinician-entered synthetic assessment",
                severityGrade=(byte?)1,causality=(string?)"Undetermined",
                interventions=(string?)"Documented observation",
                recommendations=(string?)"Case to be reviewed by HV",
                outcome=(string?)"Stable",nationalReportRequired=false,
                rowVersion=reactionJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.OK,doctorAssessed.StatusCode);
        var assessedJson=await doctorAssessed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DoctorReview",assessedJson.GetProperty("status").GetString());

        var hv=await NewClinicianAsync("st09_hv",
            "HvFirst-Ci_2026-Str0ng!","HV_MANAGER",true);
        var hv2=await NewClinicianAsync("st09_hv2",
            "HvSecond-Ci_2026-Str0ng!","HV_MANAGER",true);
        using var hvHttp=hv.Http;
        using var hv2Http=hv2.Http;
        var hvRoot=$"/api/v1/facilities/{facilityA}/hemovigilance";

        using var investigated=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/reactions/{eventId}/investigations",hv.Csrf,new {
                type="LabFollowUp",resultText="Synthetic report documented by HV",
                rowVersion=assessedJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.OK,investigated.StatusCode);
        var investigatedJson=await investigated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Investigation",investigatedJson.GetProperty("status").GetString());
        using var staleInvestigation=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/reactions/{eventId}/investigations",hv.Csrf,new {
                type="LabFollowUp",resultText="Cannot reuse stale event version",
                rowVersion=assessedJson.GetProperty("rowVersion").GetString()});
        Assert.Equal(HttpStatusCode.Conflict,staleInvestigation.StatusCode);

        using var confirmed=await ClinicalPostAsync(hv2Http,
            $"{hvRoot}/reactions/{eventId}/review",hv2.Csrf,new {
                decision="Confirmed",rowVersion=investigatedJson.GetProperty("rowVersion").GetString(),
                reason="Synthetic HV committee finding"});
        Assert.Equal(HttpStatusCode.OK,confirmed.StatusCode);
        var confirmedJson=await confirmed.Content.ReadFromJsonAsync<JsonElement>();

        using var openedRca=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/reactions/{eventId}/rca",hv.Csrf,new{});
        Assert.Equal(HttpStatusCode.OK,openedRca.StatusCode);
        var rcaJson=await openedRca.Content.ReadFromJsonAsync<JsonElement>();
        var rcaId=rcaJson.GetProperty("id").GetInt64();
        using var duplicateRca=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/reactions/{eventId}/rca",hv.Csrf,new{});
        Assert.Equal(HttpStatusCode.Conflict,duplicateRca.StatusCode);

        using var cause=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/rca/{rcaId}/causes",hv.Csrf,new {
                causeType="Process",
                description="Synthetic RCA cause selected by reviewer",isRootCause=true});
        Assert.Equal(HttpStatusCode.OK,cause.StatusCode);
        using var assignedAction=await ClinicalPostAsync(hv2Http,
            $"{hvRoot}/rca/{rcaId}/actions",hv2.Csrf,new {
                title="Verify escalation procedure",
                description="Review and document the incident SOP",
                assignedToUserId=hv.UserId,dueAt=DateTime.UtcNow.AddDays(7)});
        Assert.Equal(HttpStatusCode.OK,assignedAction.StatusCode);
        var actionJson=await assignedAction.Content.ReadFromJsonAsync<JsonElement>();
        var actionId=actionJson.GetProperty("id").GetInt64();
        var actionVersion=actionJson.GetProperty("rowVersion").GetString();
        using var completedAction=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/actions/{actionId}/complete",hv.Csrf,new {
                rowVersion=actionVersion,note=(string?)"Synthetic completion"});
        Assert.Equal(HttpStatusCode.OK,completedAction.StatusCode);
        var completedActionJson=await completedAction.Content.ReadFromJsonAsync<JsonElement>();
        using var selfVerify=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/actions/{actionId}/verify",hv.Csrf,new {
                rowVersion=completedActionJson.GetProperty("rowVersion").GetString(),
                note=(string?)null});
        Assert.Equal(HttpStatusCode.Forbidden,selfVerify.StatusCode);
        using var independentlyVerified=await ClinicalPostAsync(hv2Http,
            $"{hvRoot}/actions/{actionId}/verify",hv2.Csrf,new {
                rowVersion=completedActionJson.GetProperty("rowVersion").GetString(),
                note=(string?)"Independent verification"});
        Assert.Equal(HttpStatusCode.OK,independentlyVerified.StatusCode);

        using var rcaDetails=await hv2Http.GetAsync($"{hvRoot}/rca/{rcaId}");
        Assert.Equal(HttpStatusCode.OK,rcaDetails.StatusCode);
        var detailJson=await rcaDetails.Content.ReadFromJsonAsync<JsonElement>();
        var rcaVersion=detailJson.GetProperty("case").GetProperty("rowVersion").GetString();
        using var rcaSelfClose=await ClinicalPostAsync(hvHttp,
            $"{hvRoot}/rca/{rcaId}/close",hv.Csrf,new {
                rowVersion=rcaVersion,rootCauseSummary="Synthetic root cause summary"});
        Assert.Equal(HttpStatusCode.Forbidden,rcaSelfClose.StatusCode);
        using var rcaClosed=await ClinicalPostAsync(hv2Http,
            $"{hvRoot}/rca/{rcaId}/close",hv2.Csrf,new {
                rowVersion=rcaVersion,
                rootCauseSummary="Completed documented RCA with independently verified action"});
        Assert.Equal(HttpStatusCode.OK,rcaClosed.StatusCode);

        using var reactionClosed=await ClinicalPostAsync(hv2Http,
            $"{hvRoot}/reactions/{eventId}/review",hv2.Csrf,new {
                decision="Closed",rowVersion=confirmedJson.GetProperty("rowVersion").GetString(),
                reason="Case closed following independently verified RCA"});
        Assert.Equal(HttpStatusCode.OK,reactionClosed.StatusCode);
        await using(var hvDb=new HemoDbContext(options)){
            Assert.Equal(2,await hvDb.EventSymptoms.CountAsync(x=>x.AdverseEventId==eventId));
            Assert.Equal(1,await hvDb.PhysicianEventAssessments.CountAsync(x=>x.AdverseEventId==eventId));
            Assert.True(await hvDb.AdverseEvents.AnyAsync(x=>x.AdverseEventId==eventId&&x.Status=="Closed"));
            Assert.True(await hvDb.RcaCases.AnyAsync(x=>x.RcaCaseId==rcaId&&x.Status=="Closed"));
            Assert.True(await hvDb.CorrectiveActions.AnyAsync(x=>x.CorrectiveActionId==actionId&&
                x.Status=="Verified"&&x.VerifiedByUserId==hv2.UserId));
            Assert.True(await hvDb.AuditEvents.AnyAsync(x=>x.EntityType=="RcaCase"&&
                x.EntityId==rcaId&&x.ActionType=="Closed"));
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
