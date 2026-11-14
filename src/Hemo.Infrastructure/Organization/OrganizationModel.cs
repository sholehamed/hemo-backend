using Hemo.Domain.Organization;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Organization;

public static class OrganizationModel
{
    public static void MapOrganization(this ModelBuilder builder)
    {
        var facility = builder.Entity<Facility>();
        facility.ToTable("Facility", "org", t => t.ExcludeFromMigrations());
        facility.HasKey(x => x.FacilityId);
        facility.Property(x => x.Code).HasMaxLength(30).IsRequired();
        facility.Property(x => x.Name).HasMaxLength(200).IsRequired();
        facility.Property(x => x.FacilityType).HasMaxLength(25).IsUnicode(false).HasDefaultValue("Hospital");
        facility.Property(x => x.HisFacilityCode).HasMaxLength(60);
        facility.Property(x => x.Province).HasMaxLength(100);
        facility.Property(x => x.City).HasMaxLength(100);
        facility.Property(x => x.IsActive).HasDefaultValue(true);
        facility.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        facility.Property(x => x.RowVersion).IsRowVersion();
        facility.HasIndex(x => x.Code).IsUnique();

        var dept = builder.Entity<Department>();
        dept.ToTable("Department", "org", t => t.ExcludeFromMigrations());
        dept.HasKey(x => x.DepartmentId);
        dept.Property(x => x.Code).HasMaxLength(40).IsRequired();
        dept.Property(x => x.Name).HasMaxLength(160).IsRequired();
        dept.Property(x => x.DepartmentType).HasMaxLength(30).IsUnicode(false)
            .HasDefaultValue("Clinical");
        dept.Property(x => x.IsActive).HasDefaultValue(true);
        dept.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        dept.Property(x => x.RowVersion).IsRowVersion();
        dept.HasOne<Facility>().WithMany().HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);
        dept.HasOne<Department>().WithMany().HasForeignKey(x => x.ParentDepartmentId)
            .OnDelete(DeleteBehavior.NoAction);

        var bed = builder.Entity<Bed>();
        bed.ToTable("Bed", "org", t => t.ExcludeFromMigrations());
        bed.HasKey(x => x.BedId);
        bed.Property(x => x.BedCode).HasMaxLength(30).IsRequired();
        bed.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Active");
        bed.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        bed.Property(x => x.RowVersion).IsRowVersion();
        bed.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
