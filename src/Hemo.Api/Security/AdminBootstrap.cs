using Hemo.Domain.Organization;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
using System.Data;
using Hemo.Infrastructure.Persistence;
using Hemo.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Api.Security;

/// <summary>Explicit one-time CLI command; never runs during normal startup.</summary>
public static class AdminBootstrap
{
    public static async Task RunAsync(
        HemoDbContext db, IPasswordHasher<AccountUser> hasher,
        IConfiguration configuration, CancellationToken ct = default)
    {
        var username = configuration["HEMO_BOOTSTRAP_USERNAME"]?.Trim();
        var displayName = configuration["HEMO_BOOTSTRAP_DISPLAY_NAME"]?.Trim();
        var password = configuration["HEMO_BOOTSTRAP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 ||
            string.IsNullOrWhiteSpace(displayName) || displayName.Length > 180 ||
            string.IsNullOrEmpty(password) || password.Length < 16 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException(
                "Bootstrap variables are missing or insufficiently strong. " +
                "Provide HEMO_BOOTSTRAP_USERNAME, HEMO_BOOTSTRAP_DISPLAY_NAME, " +
                "HEMO_BOOTSTRAP_PASSWORD (16+ characters, mixed classes).");

        await using var tx = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, ct);
        var systemRoleId = await db.Roles.AsNoTracking()
            .Where(r => r.Code == "SYSTEM_ADMIN")
            .Select(r => r.RoleId).SingleAsync(ct);

        if (await db.UserRoles.AnyAsync(r => r.RoleId == systemRoleId && r.IsActive, ct))
            throw new InvalidOperationException(
                "Active system administrator already exists. Bootstrap refused.");

        if (await db.AccountUsers.AnyAsync(u => u.UserName == username, ct))
            throw new InvalidOperationException("Username already exists.");

        var user = new AccountUser
        {
            UserName = username,
            DisplayName = displayName,
            IsActive = true
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.AccountUsers.Add(user);
        await db.SaveChangesAsync(ct);

        db.UserRoles.Add(new UserRole
        {
            AccountUserId = user.AccountUserId,
            RoleId = systemRoleId,
            IsActive = true,
            EffectiveFrom = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
