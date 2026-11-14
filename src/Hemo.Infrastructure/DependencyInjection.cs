using Hemo.Application.Abstractions;
using Hemo.Application.Patients;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hemo.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Hemo");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:Hemo must be provided through configuration or secrets.");

        services.AddScoped<IAdmissionStore, AdmissionService>();
        services.AddScoped<IResourcePermission, Hemo.Infrastructure.Security.ResourceAccessService>();
        services.AddDbContext<HemoDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(15)));

        return services;
    }
}
