using System.Security.Claims;
using Hemo.Infrastructure.Organization;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Api.Security;

public sealed record NewFacilityRequest(string Code, string Name);
public sealed record NewDepartmentRequest(long FacilityId, string Code, string Name,
    long? ParentDepartmentId);
public sealed record NewBedRequest(long DepartmentId, string BedCode);
public sealed record OrgItem(long Id, string Code, string Name, long? ParentId);

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/organization")
            .RequireAuthorization("UsersManageGlobal").WithTags("Organization Admin");

        admin.MapGet("/facilities", async (HemoDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Facilities.AsNoTracking().OrderBy(x => x.Name)
                .Select(x => new OrgItem(x.FacilityId, x.Code, x.Name, null))
                .ToListAsync(ct)));

        admin.MapPost("/facilities", async (HttpContext http, IAntiforgery anti,
            NewFacilityRequest request, HemoDbContext db, HemoAuditWriter audit,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var code = request.Code?.Trim() ?? "";
            var name = request.Name?.Trim() ?? "";
            if (code.Length is < 2 or > 30 || name.Length is < 2 or > 200)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["facility"] = ["کد یا نام مرکز نامعتبر است."] });

            if (await db.Facilities.AnyAsync(x => x.Code == code, ct))
                return Results.Conflict(new { code = "duplicate_facility" });
            var entity = new Facility { Code = code, Name = name, FacilityType = "Hospital" };
            db.Facilities.Add(entity);
            await db.SaveChangesAsync(ct);
            audit.Record(CurrentId(http), entity.FacilityId, "Facility",
                entity.FacilityId, "Created");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/organization/facilities",
                new OrgItem(entity.FacilityId, entity.Code, entity.Name, null));
        });

        admin.MapPost("/departments", async (HttpContext http, IAntiforgery anti,
            NewDepartmentRequest request, HemoDbContext db, HemoAuditWriter audit,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var code = request.Code?.Trim() ?? "";
            var name = request.Name?.Trim() ?? "";
            if (code.Length is < 1 or > 40 || name.Length is < 2 or > 160 ||
                !await db.Facilities.AnyAsync(x => x.FacilityId == request.FacilityId
                    && x.IsActive, ct))
                return Results.BadRequest(new { code = "invalid_department" });
            if (request.ParentDepartmentId.HasValue &&
                !await db.Departments.AnyAsync(x =>
                    x.DepartmentId == request.ParentDepartmentId && x.FacilityId == request.FacilityId
                    && x.IsActive, ct))
                return Results.BadRequest(new { code = "invalid_parent" });

            if (await db.Departments.AnyAsync(x => x.FacilityId == request.FacilityId &&
                x.Code == code, ct))
                return Results.Conflict(new { code = "duplicate_department" });
            var entity = new Department
            {
                FacilityId = request.FacilityId,
                ParentDepartmentId = request.ParentDepartmentId,
                Code = code, Name = name
            };
            db.Departments.Add(entity);
            await db.SaveChangesAsync(ct);
            audit.Record(CurrentId(http), entity.FacilityId, "Department",
                entity.DepartmentId, "Created");
            await db.SaveChangesAsync(ct);
            return Results.Created("/api/v1/admin/organization/departments",
                new OrgItem(entity.DepartmentId, entity.Code, entity.Name, entity.FacilityId));
        });

        admin.MapPost("/beds", async (HttpContext http, IAntiforgery anti,
            NewBedRequest request, HemoDbContext db, HemoAuditWriter audit,
            CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var code = request.BedCode?.Trim() ?? "";
            var dept = await db.Departments.AsNoTracking().SingleOrDefaultAsync(
                x => x.DepartmentId == request.DepartmentId && x.IsActive, ct);
            if (dept is null || code.Length is < 1 or > 30)
                return Results.BadRequest(new { code = "invalid_bed" });
            if (await db.Beds.AnyAsync(x => x.DepartmentId == dept.DepartmentId &&
                x.BedCode == code, ct))
                return Results.Conflict(new { code = "duplicate_bed" });

            var bed = new Bed { DepartmentId = dept.DepartmentId, BedCode = code };
            db.Beds.Add(bed);
            await db.SaveChangesAsync(ct);
            audit.Record(CurrentId(http), dept.FacilityId, "Bed", bed.BedId, "Created");
            await db.SaveChangesAsync(ct);
            return Results.Created("/api/v1/admin/organization/beds",
                new OrgItem(bed.BedId, bed.BedCode, bed.Status, bed.DepartmentId));
        });

        var scope = app.MapGroup("/api/v1/organization").RequireAuthorization()
            .WithTags("Organization");
        scope.MapGet("/facilities", async (HttpContext http, ResourceAccessService access,
            CancellationToken ct) =>
        {
            var list = await access.AccessibleFacilitiesAsync(CurrentId(http), ct);
            return Results.Ok(list.Select(x => new OrgItem(x.FacilityId, x.Code, x.Name, null)));
        });

        scope.MapGet("/facilities/{facilityId:long}/departments", async (
            long facilityId, HttpContext http, HemoDbContext db, ResourceAccessService access,
            CancellationToken ct) =>
        {
            var all = await db.Departments.AsNoTracking()
                .Where(x => x.FacilityId == facilityId && x.IsActive)
                .OrderBy(x => x.Name).ToListAsync(ct);
            var allowed = new List<OrgItem>();
            foreach (var dept in all)
                if (await access.CanAsync(CurrentId(http), "patient.read",
                    facilityId, dept.DepartmentId, ct))
                    allowed.Add(new OrgItem(dept.DepartmentId, dept.Code, dept.Name, facilityId));
            return Results.Ok(allowed);
        });
        scope.MapGet("/facilities/{facilityId:long}/departments/{departmentId:long}/beds",
            async (long facilityId, long departmentId, HttpContext http,
                HemoDbContext db, ResourceAccessService access, CancellationToken ct) =>
            {
                if (!await access.CanAsync(CurrentId(http), "patient.read",
                    facilityId, departmentId, ct))
                    return Results.Forbid();
                var beds = await db.Beds.AsNoTracking()
                    .Where(x => x.DepartmentId == departmentId && x.Status == "Active"
                        && !db.EncounterLocations.Any(loc => loc.BedId == x.BedId
                            && loc.ToAt == null))
                    .OrderBy(x => x.BedCode)
                    .Select(x => new OrgItem(x.BedId, x.BedCode, "Active", departmentId))
                    .ToListAsync(ct);
                return Results.Ok(beds);
            });

        scope.MapGet("/facilities/{facilityId:long}/access-check", async (
            long facilityId, long? departmentId, HttpContext http, ResourceAccessService access,
            CancellationToken ct) =>
        {
            return await access.CanAsync(CurrentId(http), "patient.read", facilityId,
                departmentId, ct)
                ? Results.Ok(new { authorized = true })
                : Results.StatusCode(StatusCodes.Status403Forbidden);
        });
    }

    private static long CurrentId(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
