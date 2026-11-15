using Hemo.Application.Abstractions;
using Hemo.Application.Cqrs;
using Hemo.Application.Patients;
using Hemo.Domain.Patients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hemo.Api.IntegrationTests;

public sealed class CqrsApplicationTests
{
    [Fact]
    public async Task Denied_permission_never_reaches_patient_store()
    {
        var store = new SpyStore();
        var handler = new CreateAdmissionHandler(store, new FakePermission(false));
        var request = new CreateAdmissionCommand(4, 23, 41,
            new CreateAdmissionRequest("0012345678", false, "Example", "Patient",
                null, null, "U", null, "Routine", null, null));
        var result = await handler.Handle(request, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error);
        Assert.Equal(0, store.WriteCalls);
    }

    [Fact]
    public async Task Authorized_query_uses_read_port_and_returns_expected_page()
    {
        var store = new SpyStore();
        var handler = new ListAdmissionsHandler(store, new FakePermission(true));
        var result = await handler.Handle(
            new ListAdmissionsQuery(5, 23, 41, "Example", 2), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(1, store.ReadCalls);
        Assert.Equal(2, result.Value!.Page);
    }

    [Fact]
    public void Domain_and_application_assemblies_do_not_reference_ef_or_http()
    {
        static bool IsClean(System.Reflection.Assembly assembly) =>
            !assembly.GetReferencedAssemblies().Any(x => x.Name is not null &&
                (x.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                 || x.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                 || x.Name == "Hemo.Infrastructure"
                 || x.Name == "Hemo.Api"));
        Assert.True(IsClean(typeof(Patient).Assembly), "Domain has an outer dependency.");
        Assert.True(IsClean(typeof(CreateAdmissionHandler).Assembly), "Application has an outer dependency.");
    }

    [Fact]
    public void Minimal_api_composition_root_registers_all_admission_handlers()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<CreateAdmissionCommand, AdmissionOperation<AdmissionView>>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<ListAdmissionsQuery, AdmissionOperation<AdmissionPage>>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<
            IRequestHandler<VerifyIdentityCommand, AdmissionOperation<IdentityVerificationView>>>());
    }

    private sealed class FakePermission(bool result) : IResourcePermission
    {
        public Task<bool> CanAsync(long userId, string permission, long facilityId,
            long? departmentId, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class SpyStore : IAdmissionStore
    {
        public int WriteCalls { get; private set; }
        public int ReadCalls { get; private set; }

        public Task<AdmissionPage> ListAsync(long facilityId, long departmentId,
            string? search, int page, CancellationToken ct)
        {
            ReadCalls++;
            return Task.FromResult(new AdmissionPage([], page, 20, 0));
        }
        public Task<AdmissionView?> GetAsync(long facilityId, long departmentId,
            long encounterId, CancellationToken ct)
        {
            ReadCalls++;
            return Task.FromResult<AdmissionView?>(null);
        }
        public Task<AdmissionOperation<AdmissionView>> CreateAsync(
            long facilityId, long departmentId, long actor, CreateAdmissionRequest input,
            CancellationToken ct)
        {
            WriteCalls++;
            return Task.FromResult(AdmissionOperation<AdmissionView>.Fail("not_implemented"));
        }
        public Task<AdmissionOperation<AdmissionView>> UpdatePatientAsync(
            long facilityId, long departmentId, long encounterId, long actor,
            UpdatePatientRequest input, CancellationToken ct)
            => Task.FromResult(AdmissionOperation<AdmissionView>.Fail("not_implemented"));
        public Task<AdmissionOperation<WristbandView>> IssueWristbandAsync(
            long facilityId, long departmentId, long encounterId, long actor,
            CancellationToken ct)
            => Task.FromResult(AdmissionOperation<WristbandView>.Fail("not_implemented"));
        public Task<AdmissionOperation<IdentityVerificationView>> VerifyIdentityAsync(
            long facilityId, long departmentId, long encounterId, long actor,
            IdentityVerificationRequest input, CancellationToken ct)
            => Task.FromResult(AdmissionOperation<IdentityVerificationView>.Fail("not_implemented"));
    }
}
