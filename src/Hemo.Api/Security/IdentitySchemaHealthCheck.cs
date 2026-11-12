using System.Data;
using Hemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Hemo.Api.Security;

/// <summary>
/// A SQL Server being reachable is insufficient; sec.* tables must be installed.
/// This check intentionally does not query patient tables or expose identifiers.
/// </summary>
public sealed class IdentitySchemaHealthCheck(HemoDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = db.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sys.tables t
                INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                WHERE s.name = 'sec' AND t.name IN
                    ('AccountUser', 'Role', 'Permission', 'UserRole', 'RolePermission');
                """;
            command.CommandTimeout = 5;
            var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            return tableCount == 5
                ? HealthCheckResult.Healthy("Hemo identity schema is present.")
                : HealthCheckResult.Unhealthy("Hemo identity schema is incomplete.");
        }
        catch (Exception e) when (
            e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("Hemo identity schema unavailable.");
        }
    }
}
