using Hemo.Domain.Organization;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
using Hemo.Infrastructure.Organization;
using Hemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Security;

/// <summary>Evaluates a permission against the actual facility/department resource.</summary>
public sealed class ResourceAccessService(HemoAccessService grants, HemoDbContext db)
{
    public async Task<bool> CanAsync(long userId, string permission, long facilityId,
        long? departmentId, CancellationToken ct = default)
    {
        if (facilityId <= 0 || userId <= 0) return false;
        var exists = await db.Facilities.AsNoTracking()
            .AnyAsync(x => x.FacilityId == facilityId && x.IsActive, ct);
        if (!exists) return false;

        if (departmentId.HasValue && !await db.Departments.AsNoTracking()
                .AnyAsync(x => x.DepartmentId == departmentId.Value
                    && x.FacilityId == facilityId && x.IsActive, ct))
            return false;

        var entries = await grants.GetGrantsAsync(userId, ct);
        return entries.Any(x => x.Permissions.Contains(permission, StringComparer.Ordinal)
            && ((x.RoleCode == "SYSTEM_ADMIN"
                    && x.FacilityId is null && x.DepartmentId is null)
                || (x.FacilityId == facilityId
                    && (x.DepartmentId is null || x.DepartmentId == departmentId))));
    }

    public async Task<IReadOnlyList<Facility>> AccessibleFacilitiesAsync(
        long userId, CancellationToken ct = default)
    {
        var entries = await grants.GetGrantsAsync(userId, ct);
        var all = entries.Any(x => x.RoleCode == "SYSTEM_ADMIN"
            && x.FacilityId is null && x.DepartmentId is null);
        var ids = entries.Where(x => x.FacilityId.HasValue)
            .Select(x => x.FacilityId!.Value).Distinct().ToArray();

        return await db.Facilities.AsNoTracking()
            .Where(x => x.IsActive && (all || ids.Contains(x.FacilityId)))
            .OrderBy(x => x.Name).ToArrayAsync(ct);
    }
}
