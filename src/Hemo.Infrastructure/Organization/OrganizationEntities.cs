namespace Hemo.Infrastructure.Organization;

public sealed class Facility
{
    public long FacilityId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string FacilityType { get; set; } = "Hospital";
    public string? HisFacilityCode { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Department
{
    public long DepartmentId { get; set; }
    public long FacilityId { get; set; }
    public long? ParentDepartmentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string DepartmentType { get; set; } = "Clinical";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class Bed
{
    public long BedId { get; set; }
    public long DepartmentId { get; set; }
    public string BedCode { get; set; } = "";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
