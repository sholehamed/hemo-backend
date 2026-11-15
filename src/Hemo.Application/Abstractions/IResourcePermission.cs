namespace Hemo.Application.Abstractions;

/// <summary>
/// Server-authoritative, resource-scoped permission check.
/// All application use cases authorize BEFORE access to protected patient data.
/// </summary>
public interface IResourcePermission
{
    Task<bool> CanAsync(long userId, string permission, long facilityId,
        long? departmentId, CancellationToken cancellationToken = default);
}
