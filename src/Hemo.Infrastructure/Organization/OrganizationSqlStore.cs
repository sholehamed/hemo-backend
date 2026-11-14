using Hemo.Application.Cqrs;
using Hemo.Application.Organization;
using Hemo.Domain.Organization;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Organization;

/// <summary>
/// SQL adapter for organization commands and read models.
/// All writes are invoked only by global-admin authorized Minimal API routes.
/// </summary>
public sealed class OrganizationSqlStore(
    HemoDbContext db, HemoAuditWriter audit, ResourceAccessService access)
    : IOrganizationStore
{
    public async Task<IReadOnlyList<OrgItem>> ListAllFacilitiesAsync(CancellationToken ct) =>
        await db.Facilities.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new OrgItem(x.FacilityId, x.Code, x.Name, null))
            .ToArrayAsync(ct);

    public async Task<IReadOnlyList<OrgItem>> ListScopedFacilitiesAsync(
        long actor, CancellationToken ct)
    {
        var facilities = await access.AccessibleFacilitiesAsync(actor, ct);
        return facilities.Select(x => new OrgItem(x.FacilityId, x.Code, x.Name, null)).ToArray();
    }

    public async Task<IReadOnlyList<OrgItem>> ListDepartmentsAsync(
        long actor, long facilityId, CancellationToken ct)
    {
        var departments = await db.Departments.AsNoTracking()
            .Where(x => x.FacilityId == facilityId && x.IsActive)
            .OrderBy(x => x.Name).ToArrayAsync(ct);
        var allowed = new List<OrgItem>();
        foreach (var d in departments)
            if (await access.CanAsync(actor, "patient.read", facilityId, d.DepartmentId, ct))
                allowed.Add(new OrgItem(d.DepartmentId, d.Code, d.Name, facilityId));
        return allowed;
    }

    public async Task<IReadOnlyList<OrgItem>> ListAvailableBedsAsync(
        long facilityId, long departmentId, CancellationToken ct)
    {
        // Facility/department is already authorized in the Application handler.
        return await db.Beds.AsNoTracking()
            .Where(x => x.DepartmentId == departmentId && x.Status == "Active"
                && !db.EncounterLocations.Any(loc => loc.BedId == x.BedId && loc.ToAt == null))
            .OrderBy(x => x.BedCode)
            .Select(x => new OrgItem(x.BedId, x.BedCode, "Active", departmentId))
            .ToArrayAsync(ct);
    }

    public async Task<Outcome<OrgItem>> CreateFacilityAsync(
        long actor, NewFacilityRequest input, CancellationToken ct)
    {
        var code = input.Code?.Trim() ?? "";
        var name = input.Name?.Trim() ?? "";
        if (code.Length is < 2 or > 30 || name.Length is < 2 or > 200)
            return Outcome<OrgItem>.Fail("invalid_facility");
        if (await db.Facilities.AnyAsync(x => x.Code == code, ct))
            return Outcome<OrgItem>.Fail("duplicate_facility");
        var f = new Facility { Code = code, Name = name, FacilityType = "Hospital" };
        db.Facilities.Add(f);
        await db.SaveChangesAsync(ct);
        audit.Record(actor, f.FacilityId, "Facility", f.FacilityId, "Created");
        await db.SaveChangesAsync(ct);
        return Outcome<OrgItem>.Ok(new OrgItem(f.FacilityId, f.Code, f.Name, null));
    }

    public async Task<Outcome<OrgItem>> CreateDepartmentAsync(
        long actor, NewDepartmentRequest input, CancellationToken ct)
    {
        var code = input.Code?.Trim() ?? "";
        var name = input.Name?.Trim() ?? "";
        if (code.Length is < 1 or > 40 || name.Length is < 2 or > 160 ||
            !await db.Facilities.AnyAsync(x => x.FacilityId == input.FacilityId && x.IsActive, ct))
            return Outcome<OrgItem>.Fail("invalid_department");
        if (input.ParentDepartmentId.HasValue &&
            !await db.Departments.AnyAsync(x => x.DepartmentId == input.ParentDepartmentId
                && x.FacilityId == input.FacilityId && x.IsActive, ct))
            return Outcome<OrgItem>.Fail("invalid_parent");
        if (await db.Departments.AnyAsync(x =>
            x.FacilityId == input.FacilityId && x.Code == code, ct))
            return Outcome<OrgItem>.Fail("duplicate_department");

        var d = new Department
        {
            FacilityId = input.FacilityId, ParentDepartmentId = input.ParentDepartmentId,
            Code = code, Name = name
        };
        db.Departments.Add(d);
        await db.SaveChangesAsync(ct);
        audit.Record(actor, d.FacilityId, "Department", d.DepartmentId, "Created");
        await db.SaveChangesAsync(ct);
        return Outcome<OrgItem>.Ok(new OrgItem(
            d.DepartmentId, d.Code, d.Name, d.FacilityId));
    }

    public async Task<Outcome<OrgItem>> CreateBedAsync(
        long actor, NewBedRequest input, CancellationToken ct)
    {
        var code = input.BedCode?.Trim() ?? "";
        var d = await db.Departments.AsNoTracking().SingleOrDefaultAsync(
            x => x.DepartmentId == input.DepartmentId && x.IsActive, ct);
        if (d is null || code.Length is < 1 or > 30)
            return Outcome<OrgItem>.Fail("invalid_bed");
        if (await db.Beds.AnyAsync(x =>
            x.DepartmentId == d.DepartmentId && x.BedCode == code, ct))
            return Outcome<OrgItem>.Fail("duplicate_bed");

        var bed = new Bed { DepartmentId = d.DepartmentId, BedCode = code };
        db.Beds.Add(bed);
        await db.SaveChangesAsync(ct);
        audit.Record(actor, d.FacilityId, "Bed", bed.BedId, "Created");
        await db.SaveChangesAsync(ct);
        return Outcome<OrgItem>.Ok(new OrgItem(
            bed.BedId, bed.BedCode, bed.Status, bed.DepartmentId));
    }
}
