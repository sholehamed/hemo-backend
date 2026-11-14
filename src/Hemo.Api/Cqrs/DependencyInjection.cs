using Hemo.Application.Cqrs;
using Hemo.Application.Patients;

namespace Hemo.Api.Cqrs;

/// <summary>Explicit composition root: every use case has one dedicated handler.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddHemoCqrs(this IServiceCollection services)
    {
        services.AddScoped<IRequestHandler<ListAdmissionsQuery, AdmissionOperation<AdmissionPage>>, ListAdmissionsHandler>();
        services.AddScoped<IRequestHandler<GetAdmissionQuery, AdmissionOperation<AdmissionView>>, GetAdmissionHandler>();
        services.AddScoped<IRequestHandler<CreateAdmissionCommand, AdmissionOperation<AdmissionView>>, CreateAdmissionHandler>();
        services.AddScoped<IRequestHandler<UpdatePatientCommand, AdmissionOperation<AdmissionView>>, UpdatePatientHandler>();
        services.AddScoped<IRequestHandler<IssueWristbandCommand, AdmissionOperation<WristbandView>>, IssueWristbandHandler>();
        services.AddScoped<IRequestHandler<VerifyIdentityCommand, AdmissionOperation<IdentityVerificationView>>, VerifyIdentityHandler>();
        return services;
    }
}
