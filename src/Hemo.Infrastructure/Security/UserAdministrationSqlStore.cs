using Hemo.Application.Cqrs;
using Hemo.Application.Users;
using Hemo.Domain.Security;
using Hemo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Security;

/// <summary>Database adapter: account hashing, scoped grants and revocation.</summary>
public sealed class UserAdministrationSqlStore(
    HemoDbContext db, HemoAuditWriter audit, IPasswordHasher<AccountUser> hasher)
    : IUserAdministrationStore
{
    public async Task<Outcome<IReadOnlyList<ManagedUser>>> ListAsync(
        string? search, int page, CancellationToken ct)
    {
        var query = db.AccountUsers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            if (q.Length > 100)
                return Outcome<IReadOnlyList<ManagedUser>>.Fail("search_too_long");
            query = query.Where(x => x.UserName.Contains(q) || x.DisplayName.Contains(q));
        }
        IReadOnlyList<ManagedUser> users = await query.OrderBy(x => x.AccountUserId)
            .Skip((Math.Clamp(page, 1, 10000) - 1) * 20).Take(20)
            .Select(x => new ManagedUser(x.AccountUserId, x.UserName,
                x.DisplayName, x.IsActive)).ToArrayAsync(ct);
        return Outcome<IReadOnlyList<ManagedUser>>.Ok(users);
    }

    public async Task<IReadOnlyList<ManagedRole>> ListRolesAsync(CancellationToken ct) =>
        await db.Roles.AsNoTracking()
            .Select(r => new ManagedRole(r.RoleId, r.Code, r.Name))
            .OrderBy(r => r.Code).ToArrayAsync(ct);

    public async Task<Outcome<ManagedUser>> CreateAsync(
        long actor, CreateUserRequest request, CancellationToken ct)
    {
        var username = request.UserName?.Trim() ?? "";
        var display = request.DisplayName?.Trim() ?? "";
        var password = request.InitialPassword ?? "";
        if (username.Length is < 3 or > 100 || display.Length is < 2 or > 180 ||
            password.Length is < 16 or > 512 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            return Outcome<ManagedUser>.Fail("invalid_user");
        if (await db.AccountUsers.AnyAsync(x => x.UserName == username, ct))
            return Outcome<ManagedUser>.Fail("duplicate_user");

        var user = new AccountUser { UserName = username, DisplayName = display };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.AccountUsers.Add(user);
        await db.SaveChangesAsync(ct);
        audit.Record(actor, null, "AccountUser", user.AccountUserId, "Created");
        await db.SaveChangesAsync(ct);
        return Outcome<ManagedUser>.Ok(new ManagedUser(
            user.AccountUserId, user.UserName, user.DisplayName, user.IsActive));
    }

    public async Task<Outcome<GrantReceipt>> GrantAsync(long actor, long userId,
        GrantUserRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.RoleId == request.RoleId, ct);
        if (role is null || !await db.AccountUsers.AnyAsync(
            x => x.AccountUserId == userId && x.IsActive, ct))
            return Outcome<GrantReceipt>.Fail("invalid_assignment");

        if (role.Code == "SYSTEM_ADMIN")
        {
            if (request.FacilityId.HasValue || request.DepartmentId.HasValue)
                return Outcome<GrantReceipt>.Fail("invalid_system_scope");
        }
        else
        {
            if (!request.FacilityId.HasValue ||
                !await db.Facilities.AnyAsync(x => x.FacilityId == request.FacilityId
                    && x.IsActive, ct))
                return Outcome<GrantReceipt>.Fail("facility_required");
            if (request.DepartmentId.HasValue &&
                !await db.Departments.AnyAsync(x => x.DepartmentId == request.DepartmentId
                    && x.FacilityId == request.FacilityId && x.IsActive, ct))
                return Outcome<GrantReceipt>.Fail("invalid_department_scope");
        }

        if (await db.UserRoles.AnyAsync(x =>
            x.AccountUserId == userId && x.RoleId == role.RoleId && x.IsActive &&
            x.FacilityId == request.FacilityId && x.DepartmentId == request.DepartmentId, ct))
            return Outcome<GrantReceipt>.Fail("grant_exists");

        var grant = new UserRole
        {
            AccountUserId = userId, RoleId = request.RoleId,
            FacilityId = request.FacilityId, DepartmentId = request.DepartmentId,
            EffectiveFrom = DateTime.UtcNow
        };
        db.UserRoles.Add(grant);
        await db.SaveChangesAsync(ct);
        audit.Record(actor, request.FacilityId, "UserRole", grant.UserRoleId, "Assigned");
        await db.SaveChangesAsync(ct);
        return Outcome<GrantReceipt>.Ok(new GrantReceipt(grant.UserRoleId));
    }

    public async Task<Outcome<bool>> DeactivateAsync(
        long actor, long userId, CancellationToken ct)
    {
        var user = await db.AccountUsers.SingleOrDefaultAsync(
            x => x.AccountUserId == userId, ct);
        if (user is null) return Outcome<bool>.Fail("not_found");
        if (!user.IsActive) return Outcome<bool>.Ok(true);

        var adminRoleId = await db.Roles.Where(x => x.Code == "SYSTEM_ADMIN")
            .Select(x => x.RoleId).SingleAsync(ct);
        var otherAdmins = await db.UserRoles.AsNoTracking()
            .Where(x => x.RoleId == adminRoleId && x.IsActive &&
                x.FacilityId == null && x.DepartmentId == null &&
                x.AccountUserId != userId)
            .Join(db.AccountUsers.AsNoTracking(), x => x.AccountUserId,
                u => u.AccountUserId, (r, u) => u)
            .AnyAsync(x => x.IsActive, ct);
        var isAdmin = await db.UserRoles.AnyAsync(x =>
            x.AccountUserId == userId && x.RoleId == adminRoleId &&
            x.IsActive && x.FacilityId == null, ct);
        if (isAdmin && !otherAdmins)
            return Outcome<bool>.Fail("last_system_admin");

        user.IsActive = false;
        var sessions = await db.UserSessions.Where(x => x.AccountUserId == userId
            && x.RevokedAt == null).ToListAsync(ct);
        foreach (var session in sessions) session.RevokedAt = DateTime.UtcNow;
        audit.Record(actor, null, "AccountUser", userId, "Deactivated");
        await db.SaveChangesAsync(ct);
        return Outcome<bool>.Ok(true);
    }
}
