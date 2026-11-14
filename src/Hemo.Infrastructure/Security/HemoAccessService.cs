using Hemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Security;

public sealed record RoleGrant(string RoleCode, long? FacilityId, long? DepartmentId,
    IReadOnlyList<string> Permissions);

public sealed class HemoAccessService(HemoDbContext db)
{
    public async Task<IReadOnlyList<RoleGrant>> GetGrantsAsync(long userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var active = await (
            from grant in db.UserRoles.AsNoTracking()
            join role in db.Roles.AsNoTracking() on grant.RoleId equals role.RoleId
            where grant.AccountUserId == userId && grant.IsActive &&
                  grant.EffectiveFrom <= now &&
                  (grant.EffectiveUntil == null || grant.EffectiveUntil > now)
            select new { grant.RoleId, role.Code, grant.FacilityId, grant.DepartmentId }
        ).ToListAsync(ct);

        var roleIds = active.Select(x => x.RoleId).Distinct().ToArray();
        var permissions = await (
            from mapping in db.RolePermissions.AsNoTracking()
            join permission in db.Permissions.AsNoTracking()
                on mapping.PermissionId equals permission.PermissionId
            where roleIds.Contains(mapping.RoleId)
            select new { mapping.RoleId, permission.Code }
        ).ToListAsync(ct);

        return active.Select(x => new RoleGrant(
            x.Code, x.FacilityId, x.DepartmentId,
            permissions.Where(p => p.RoleId == x.RoleId)
                .Select(p => p.Code).Distinct().OrderBy(p => p).ToArray()))
            .ToArray();
    }

    // Only unscoped role grants authorize global administrative endpoints.
    // Clinical endpoints must additionally enforce facility/department resource scope.
    public async Task<bool> HasGlobalPermissionAsync(
        long userId, string code, CancellationToken ct = default)
    {
        var grants = await GetGrantsAsync(userId, ct);
        return grants.Any(x => x.RoleCode == "SYSTEM_ADMIN" && x.FacilityId is null && x.DepartmentId is null
            && x.Permissions.Contains(code, StringComparer.Ordinal));
    }
}
