using Hemo.Infrastructure.Security;
using Hemo.Infrastructure.Organization;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Persistence;

/// <summary>
/// Maps existing Hemo sec.* identity tables; baseline SQL remains their DDL owner.
/// No clinical entities are mapped until the schema reconciliation stage.
/// </summary>
public sealed class HemoDbContext(DbContextOptions<HemoDbContext> options) : DbContext(options)
{
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<HemoUserSession> UserSessions => Set<HemoUserSession>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AccountUser> AccountUsers => Set<AccountUser>();
    public DbSet<HemoRole> Roles => Set<HemoRole>();
    public DbSet<HemoPermission> Permissions => Set<HemoPermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.MapOrganization();

        var sessions = modelBuilder.Entity<HemoUserSession>();
        sessions.ToTable("UserSession", "sec", table => table.ExcludeFromMigrations());
        sessions.HasKey(x => x.UserSessionId);
        sessions.Property(x => x.RefreshTokenHash).HasColumnType("varbinary(32)").IsRequired();
        sessions.Property(x => x.IssuedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        sessions.Property(x => x.ExpiresAt).HasColumnType("datetime2(3)");
        sessions.Property(x => x.RevokedAt).HasColumnType("datetime2(3)");
        sessions.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        sessions.Property(x => x.ClientIp).HasMaxLength(45).IsUnicode(false);
        sessions.Property(x => x.RowVersion).IsRowVersion();
        sessions.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.AccountUserId)
            .OnDelete(DeleteBehavior.NoAction);

        var audit = modelBuilder.Entity<AuditEvent>();
        audit.ToTable("AuditEvent", "audit", table => table.ExcludeFromMigrations());
        audit.HasKey(x => x.AuditEventId);
        audit.Property(x => x.EntityType).HasMaxLength(120).IsUnicode(false);
        audit.Property(x => x.ActionType).HasMaxLength(80).IsUnicode(false);
        audit.Property(x => x.RemoteAddress).HasMaxLength(45).IsUnicode(false);
        audit.Property(x => x.EventDetails).HasMaxLength(1600);
        audit.Property(x => x.OccurredAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        audit.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.AccountUserId)
            .OnDelete(DeleteBehavior.NoAction);
        audit.HasOne<Facility>().WithMany().HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);
        var users = modelBuilder.Entity<AccountUser>();
        users.ToTable("AccountUser", "sec", table => table.ExcludeFromMigrations());
        users.HasKey(x => x.AccountUserId);
        users.Property(x => x.UserName).HasMaxLength(100).IsRequired();
        users.Property(x => x.DisplayName).HasMaxLength(180).IsRequired();
        users.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        users.Property(x => x.ExternalIdentityId).HasMaxLength(180);
        users.Property(x => x.Email).HasMaxLength(254);
        users.Property(x => x.PersonnelCode).HasMaxLength(70);
        users.Property(x => x.IsActive).HasDefaultValue(true);
        users.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");
        users.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        users.Property(x => x.RowVersion).IsRowVersion();
        users.HasIndex(x => x.UserName).IsUnique();

        var roles = modelBuilder.Entity<HemoRole>();
        roles.ToTable("Role", "sec", table => table.ExcludeFromMigrations());
        roles.HasKey(x => x.RoleId);
        roles.Property(x => x.Code).HasMaxLength(45).IsUnicode(false).IsRequired();
        roles.Property(x => x.Name).HasMaxLength(130).IsRequired();
        roles.Property(x => x.IsSystem).HasDefaultValue(true);
        roles.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        roles.Property(x => x.RowVersion).IsRowVersion();
        roles.HasIndex(x => x.Code).IsUnique();

        var permissions = modelBuilder.Entity<HemoPermission>();
        permissions.ToTable("Permission", "sec", table => table.ExcludeFromMigrations());
        permissions.HasKey(x => x.PermissionId);
        permissions.Property(x => x.Code).HasMaxLength(120).IsUnicode(false).IsRequired();
        permissions.Property(x => x.Description).HasMaxLength(260).IsRequired();
        permissions.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        permissions.Property(x => x.RowVersion).IsRowVersion();
        permissions.HasIndex(x => x.Code).IsUnique();

        var userRoles = modelBuilder.Entity<UserRole>();
        userRoles.ToTable("UserRole", "sec", table => table.ExcludeFromMigrations());
        userRoles.HasKey(x => x.UserRoleId);
        userRoles.Property(x => x.EffectiveFrom).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        userRoles.Property(x => x.EffectiveUntil).HasColumnType("datetime2(3)");
        userRoles.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        userRoles.Property(x => x.IsActive).HasDefaultValue(true);
        userRoles.Property(x => x.RowVersion).IsRowVersion();
        userRoles.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.AccountUserId)
            .OnDelete(DeleteBehavior.NoAction);
        userRoles.HasOne<HemoRole>().WithMany().HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.NoAction);

        var rolePermissions = modelBuilder.Entity<RolePermission>();
        rolePermissions.ToTable("RolePermission", "sec", table => table.ExcludeFromMigrations());
        rolePermissions.HasKey(x => x.RolePermissionId);
        rolePermissions.HasOne<HemoRole>().WithMany().HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.NoAction);
        rolePermissions.HasOne<HemoPermission>().WithMany().HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.NoAction);
        rolePermissions.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
    }
}
