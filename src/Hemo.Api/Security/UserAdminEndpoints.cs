using System.Security.Claims;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Api.Security;

public sealed record CreateUserRequest(string UserName, string DisplayName, string InitialPassword);
public sealed record GrantUserRoleRequest(long RoleId, long? FacilityId, long? DepartmentId);
public sealed record ManagedUser(long Id, string UserName, string DisplayName, bool IsActive);

public static class UserAdminEndpoints
{
    public static void MapUserAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/users")
            .RequireAuthorization("UsersManageGlobal").WithTags("User Admin");

        group.MapGet("/", async (HemoDbContext db, string? search, int? page,
            CancellationToken ct) =>
        {
            var query = db.AccountUsers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                if (q.Length > 100) return Results.BadRequest(new { code = "search_too_long" });
                query = query.Where(x => x.UserName.Contains(q) || x.DisplayName.Contains(q));
            }
            var items = await query.OrderBy(x => x.AccountUserId)
                .Skip((Math.Clamp(page ?? 1, 1, 10000) - 1) * 20).Take(20)
                .Select(x => new ManagedUser(x.AccountUserId, x.UserName, x.DisplayName, x.IsActive))
                .ToListAsync(ct);
            return Results.Ok(items);
        });

        group.MapGet("/roles", async (HemoDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Roles.AsNoTracking()
                .Select(r => new { r.RoleId, r.Code, r.Name })
                .OrderBy(r => r.Code).ToArrayAsync(ct)));

        group.MapPost("/", async (HttpContext http, IAntiforgery anti,
            CreateUserRequest request, HemoDbContext db, HemoAuditWriter audit,
            IPasswordHasher<AccountUser> hasher, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var username = request.UserName?.Trim() ?? "";
            var display = request.DisplayName?.Trim() ?? "";
            var password = request.InitialPassword ?? "";
            if (username.Length is < 3 or > 100 || display.Length is < 2 or > 180 ||
                password.Length < 16 || password.Length > 512 ||
                !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
                !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["user"] = ["مشخصات کاربر یا رمز اولیه معتبر نیست."] });
            if (await db.AccountUsers.AnyAsync(x => x.UserName == username, ct))
                return Results.Conflict(new { code = "duplicate_user" });

            var user = new AccountUser { UserName = username, DisplayName = display };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.AccountUsers.Add(user);
            await db.SaveChangesAsync(ct);
            audit.Record(CurrentId(http), null, "AccountUser", user.AccountUserId, "Created");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/users/{user.AccountUserId}",
                new ManagedUser(user.AccountUserId, user.UserName, user.DisplayName, user.IsActive));
        });

        group.MapPost("/{userId:long}/grants", async (long userId,
            GrantUserRoleRequest request, HttpContext http, IAntiforgery anti,
            HemoDbContext db, HemoAuditWriter audit, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(
                x => x.RoleId == request.RoleId, ct);
            if (role is null || !await db.AccountUsers.AnyAsync(
                x => x.AccountUserId == userId && x.IsActive, ct))
                return Results.BadRequest(new { code = "invalid_assignment" });

            if (role.Code == "SYSTEM_ADMIN")
            {
                if (request.FacilityId.HasValue || request.DepartmentId.HasValue)
                    return Results.BadRequest(new { code = "invalid_system_scope" });
            }
            else
            {
                if (!request.FacilityId.HasValue ||
                    !await db.Facilities.AnyAsync(x => x.FacilityId == request.FacilityId
                        && x.IsActive, ct))
                    return Results.BadRequest(new { code = "facility_required" });
                if (request.DepartmentId.HasValue &&
                    !await db.Departments.AnyAsync(x => x.DepartmentId == request.DepartmentId
                        && x.FacilityId == request.FacilityId && x.IsActive, ct))
                    return Results.BadRequest(new { code = "invalid_department_scope" });
            }

            if (await db.UserRoles.AnyAsync(x =>
                x.AccountUserId == userId && x.RoleId == role.RoleId && x.IsActive &&
                x.FacilityId == request.FacilityId && x.DepartmentId == request.DepartmentId, ct))
                return Results.Conflict(new { code = "grant_exists" });

            var grant = new UserRole
            {
                AccountUserId = userId, RoleId = request.RoleId,
                FacilityId = request.FacilityId, DepartmentId = request.DepartmentId,
                EffectiveFrom = DateTime.UtcNow
            };
            db.UserRoles.Add(grant);
            await db.SaveChangesAsync(ct);
            audit.Record(CurrentId(http), request.FacilityId, "UserRole",
                grant.UserRoleId, "Assigned");
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/admin/users/{userId}/grants",
                new { grant.UserRoleId });
        });

        group.MapPost("/{userId:long}/deactivate", async (
            long userId, HttpContext http, IAntiforgery anti, HemoDbContext db,
            HemoAuditWriter audit, CancellationToken ct) =>
        {
            if (!await MutationCsrf.ValidAsync(http, anti))
                return Results.BadRequest(new { code = "invalid_csrf" });
            var user = await db.AccountUsers.SingleOrDefaultAsync(x => x.AccountUserId == userId, ct);
            if (user is null) return Results.NotFound();
            if (!user.IsActive) return Results.NoContent();

            var systemRoleId = await db.Roles.Where(x => x.Code == "SYSTEM_ADMIN")
                .Select(x => x.RoleId).SingleAsync(ct);
            var otherAdmins = await db.UserRoles.AsNoTracking()
                .Where(x => x.RoleId == systemRoleId && x.IsActive &&
                    x.FacilityId == null && x.DepartmentId == null &&
                    x.AccountUserId != userId)
                .Join(db.AccountUsers.AsNoTracking(), x => x.AccountUserId,
                    u => u.AccountUserId, (r, u) => u)
                .AnyAsync(x => x.IsActive, ct);
            var isAdmin = await db.UserRoles.AnyAsync(x => x.AccountUserId == userId &&
                x.RoleId == systemRoleId && x.IsActive && x.FacilityId == null, ct);
            if (isAdmin && !otherAdmins)
                return Results.Conflict(new { code = "last_system_admin" });

            user.IsActive = false;
            var sessions = await db.UserSessions.Where(x => x.AccountUserId == userId
                && x.RevokedAt == null).ToListAsync(ct);
            foreach (var session in sessions) session.RevokedAt = DateTime.UtcNow;
            audit.Record(CurrentId(http), null, "AccountUser", userId, "Deactivated");
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
    }

    private static long CurrentId(HttpContext http) =>
        long.Parse(http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
