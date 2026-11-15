using System.Data;
using System.Security.Cryptography;
using Hemo.Application.Authentication;
using Hemo.Application.Cqrs;
using Hemo.Domain.Security;
using Hemo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Security;

/// <summary>SQL Server adapter for authenticated BFF sessions, grants and one-time bootstrap.</summary>
public sealed class AuthenticationSqlStore(
    HemoDbContext db, IPasswordHasher<AccountUser> hasher, HemoAccessService access)
    : IAuthenticationStore
{
    public async Task<Outcome<SignInTicket>> AuthenticateAsync(
        string userName, string password, string? remoteIp, CancellationToken ct)
    {
        var user = await db.AccountUsers.SingleOrDefaultAsync(
            x => x.UserName == userName, ct);
        if (user is null || !user.IsActive)
            return Outcome<SignInTicket>.Fail("unauthorized");

        var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            return Outcome<SignInTicket>.Fail("unauthorized");
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, password);

        var now = DateTime.UtcNow;
        var session = new HemoUserSession
        {
            AccountUserId = user.AccountUserId,
            RefreshTokenHash = RandomNumberGenerator.GetBytes(32),
            IssuedAt = now, ExpiresAt = now.AddHours(8),
            ClientIp = remoteIp
        };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync(ct);
        db.AuditEvents.Add(new AuditEvent
        {
            AccountUserId = user.AccountUserId, EntityType = "Session",
            EntityId = session.UserSessionId, ActionType = "Login", OccurredAt = now
        });
        await db.SaveChangesAsync(ct);
        return Outcome<SignInTicket>.Ok(new SignInTicket(
            user.AccountUserId, user.UserName, session.UserSessionId));
    }

    public async Task<Outcome<SessionView>> GetSessionAsync(long userId, CancellationToken ct)
    {
        var user = await db.AccountUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountUserId == userId && x.IsActive, ct);
        if (user is null) return Outcome<SessionView>.Fail("unauthorized");

        var grants = await access.GetGrantsAsync(userId, ct);
        return Outcome<SessionView>.Ok(new SessionView(
            user.AccountUserId, user.UserName, user.DisplayName,
            grants.Select(g => new SessionGrant(g.RoleCode, g.FacilityId,
                g.DepartmentId, g.Permissions)).ToArray()));
    }

    public async Task<bool> ValidateSessionAsync(
        long userId, long sessionId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await db.UserSessions.AsNoTracking().AnyAsync(s =>
            s.UserSessionId == sessionId && s.AccountUserId == userId
            && s.RevokedAt == null && s.ExpiresAt > now
            && db.AccountUsers.Any(u => u.AccountUserId == s.AccountUserId && u.IsActive), ct);
    }

    public async Task<Outcome<bool>> RevokeSessionAsync(
        long userId, long sessionId, CancellationToken ct)
    {
        var session = await db.UserSessions.SingleOrDefaultAsync(s =>
            s.UserSessionId == sessionId && s.AccountUserId == userId, ct);
        if (session is null) return Outcome<bool>.Fail("unauthorized");
        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTime.UtcNow;
            db.AuditEvents.Add(new AuditEvent
            {
                AccountUserId = userId, EntityType = "Session",
                EntityId = sessionId, ActionType = "Logout", OccurredAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }
        return Outcome<bool>.Ok(true);
    }

    public Task<bool> HasGlobalPermissionAsync(long userId, string code, CancellationToken ct) =>
        access.HasGlobalPermissionAsync(userId, code, ct);

    public async Task<Outcome<bool>> BootstrapSystemAdminAsync(
        string userName, string displayName, string password, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var systemRoleId = await db.Roles.AsNoTracking()
            .Where(r => r.Code == "SYSTEM_ADMIN").Select(r => r.RoleId).SingleAsync(ct);
        if (await db.UserRoles.AnyAsync(r => r.RoleId == systemRoleId && r.IsActive, ct))
            return Outcome<bool>.Fail("system_admin_exists");
        if (await db.AccountUsers.AnyAsync(u => u.UserName == userName, ct))
            return Outcome<bool>.Fail("username_exists");

        var user = new AccountUser
        {
            UserName = userName, DisplayName = displayName, IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.AccountUsers.Add(user);
        await db.SaveChangesAsync(ct);
        db.UserRoles.Add(new UserRole
        {
            AccountUserId = user.AccountUserId, RoleId = systemRoleId,
            IsActive = true, EffectiveFrom = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Outcome<bool>.Ok(true);
    }
}
