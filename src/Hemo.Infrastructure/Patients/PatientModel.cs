using Hemo.Infrastructure.Organization;
using Hemo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Patients;

public static class PatientModel
{
    public static void MapPatients(this ModelBuilder builder)
    {
        var patient = builder.Entity<Patient>();
        patient.ToTable("Patient", "patient", t => t.ExcludeFromMigrations());
        patient.HasKey(x => x.PatientId);
        patient.Property(x => x.NationalId).HasMaxLength(10).IsUnicode(false);
        patient.Property(x => x.UnknownIdentityCode).HasMaxLength(70);
        patient.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        patient.Property(x => x.LastName).HasMaxLength(120).IsRequired();
        patient.Property(x => x.FatherName).HasMaxLength(100);
        patient.Property(x => x.BirthDate).HasColumnType("date");
        patient.Property(x => x.Sex).HasMaxLength(1).IsUnicode(false).HasDefaultValue("U");
        patient.Property(x => x.Mobile).HasMaxLength(30);
        patient.Property(x => x.IsActive).HasDefaultValue(true);
        patient.Property(x => x.IsUnknown).HasDefaultValue(false);
        patient.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        patient.Property(x => x.RowVersion).IsRowVersion();
        patient.HasIndex(x => x.NationalId).IsUnique().HasFilter("[NationalId] IS NOT NULL");

        var identifier = builder.Entity<PatientIdentifier>();
        identifier.ToTable("PatientIdentifier", "patient", t => t.ExcludeFromMigrations());
        identifier.HasKey(x => x.PatientIdentifierId);
        identifier.Property(x => x.IdentifierType).HasMaxLength(30).IsUnicode(false);
        identifier.Property(x => x.IdentifierValue).HasMaxLength(90);
        identifier.Property(x => x.SourceSystem).HasMaxLength(60);
        identifier.Property(x => x.IsVerified).HasDefaultValue(false);
        identifier.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        identifier.Property(x => x.RowVersion).IsRowVersion();
        identifier.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);
        identifier.HasOne<Facility>().WithMany().HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);

        var encounter = builder.Entity<Encounter>();
        encounter.ToTable("Encounter", "patient", t => t.ExcludeFromMigrations());
        encounter.HasKey(x => x.EncounterId);
        encounter.Property(x => x.HisEncounterNo).HasMaxLength(80);
        encounter.Property(x => x.AdmissionType).HasMaxLength(25).IsUnicode(false).HasDefaultValue("Routine");
        encounter.Property(x => x.AdmittedAt).HasColumnType("datetime2(3)");
        encounter.Property(x => x.DischargedAt).HasColumnType("datetime2(3)");
        encounter.Property(x => x.Status).HasMaxLength(25).IsUnicode(false).HasDefaultValue("Admitted");
        encounter.Property(x => x.AdmissionNote).HasMaxLength(1000);
        encounter.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        encounter.Property(x => x.RowVersion).IsRowVersion();
        encounter.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.NoAction);
        encounter.HasOne<Facility>().WithMany().HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.NoAction);
        encounter.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.AttendingPhysicianId)
            .OnDelete(DeleteBehavior.NoAction);

        var loc = builder.Entity<EncounterLocation>();
        loc.ToTable("EncounterLocation", "patient", t => t.ExcludeFromMigrations());
        loc.HasKey(x => x.EncounterLocationId);
        loc.Property(x => x.FromAt).HasColumnType("datetime2(3)");
        loc.Property(x => x.ToAt).HasColumnType("datetime2(3)");
        loc.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        loc.Property(x => x.RowVersion).IsRowVersion();
        loc.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.NoAction);
        loc.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);
        loc.HasOne<Bed>().WithMany().HasForeignKey(x => x.BedId)
            .OnDelete(DeleteBehavior.NoAction);
        loc.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.RecordedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        var band = builder.Entity<Wristband>();
        band.ToTable("Wristband", "patient", t => t.ExcludeFromMigrations());
        band.HasKey(x => x.WristbandId);
        band.Property(x => x.Barcode).HasMaxLength(100);
        band.Property(x => x.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Active");
        band.Property(x => x.PrintedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        band.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        band.Property(x => x.RowVersion).IsRowVersion();
        band.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.NoAction);
        band.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.PrintedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
        band.HasIndex(x => x.Barcode).IsUnique();
        band.HasIndex(x => x.EncounterId).IsUnique()
            .HasFilter("[Status] = 'Active'");

        var verify = builder.Entity<IdentityVerification>();
        verify.ToTable("IdentityVerification", "patient", t => t.ExcludeFromMigrations());
        verify.HasKey(x => x.IdentityVerificationId);
        verify.Property(x => x.VerifiedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        verify.Property(x => x.FirstIdentifier).HasMaxLength(100);
        verify.Property(x => x.SecondIdentifier).HasMaxLength(100);
        verify.Property(x => x.Status).HasMaxLength(20).IsUnicode(false);
        verify.Property(x => x.MismatchReason).HasMaxLength(800);
        verify.Property(x => x.ChecklistJson).HasColumnType("nvarchar(max)");
        verify.Property(x => x.CreatedAt).HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        verify.Property(x => x.RowVersion).IsRowVersion();
        verify.HasOne<Encounter>().WithMany().HasForeignKey(x => x.EncounterId)
            .OnDelete(DeleteBehavior.NoAction);
        verify.HasOne<Wristband>().WithMany().HasForeignKey(x => x.WristbandId)
            .OnDelete(DeleteBehavior.NoAction);
        verify.HasOne<AccountUser>().WithMany().HasForeignKey(x => x.VerifiedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
