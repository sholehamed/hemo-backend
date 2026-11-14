namespace Hemo.Domain.Security;

// The SQL baseline is the schema owner. These EF models map its existing sec tables.
public sealed class AccountUser
{
    public long AccountUserId { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? ExternalIdentityId { get; set; }
    public string? Email { get; set; }
    public string? PersonnelCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HemoRole
{
    public long RoleId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsSystem { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class HemoPermission
{
    public long PermissionId { get; set; }
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class RolePermission
{
    public long RolePermissionId { get; set; }
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
}

public sealed class UserRole
{
    public long UserRoleId { get; set; }
    public long AccountUserId { get; set; }
    public long RoleId { get; set; }
    public long? FacilityId { get; set; }
    public long? DepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveUntil { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
