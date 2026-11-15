using Hemo.Domain.Clinical;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Clinical;

public static class ClinicalRequestModel
{
    public static void MapClinicalRequests(this ModelBuilder model)
    {
        var req = model.Entity<BloodRequest>();
        req.ToTable("BloodRequest","clinical",t=>t.ExcludeFromMigrations());
        req.HasKey(x=>x.BloodRequestId);
        req.Property(x=>x.RequestNo).HasMaxLength(55).IsRequired();
        req.Property(x=>x.RequestSource).HasMaxLength(25).IsUnicode(false);
        req.Property(x=>x.RequestType).HasMaxLength(35).IsUnicode(false);
        req.Property(x=>x.Priority).HasMaxLength(20).IsUnicode(false);
        req.Property(x=>x.Status).HasMaxLength(35).IsUnicode(false);
        req.Property(x=>x.Indication).HasMaxLength(2000);
        req.Property(x=>x.Diagnosis).HasMaxLength(1000);
        req.Property(x=>x.ExternalRequestNo).HasMaxLength(75);
        req.Property(x=>x.RequestedAt).HasColumnType("datetime2(3)");
        req.Property(x=>x.ExpectedAt).HasColumnType("datetime2(3)");
        req.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        req.Property(x=>x.RowVersion).IsRowVersion();
        req.HasOne<Encounter>().WithMany().HasForeignKey(x=>x.EncounterId).OnDelete(DeleteBehavior.NoAction);
        req.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);
        req.HasIndex(x=>x.RequestNo).IsUnique();

        var item=model.Entity<BloodRequestItem>();
        item.ToTable("BloodRequestItem","clinical",t=>t.ExcludeFromMigrations());
        item.HasKey(x=>x.BloodRequestItemId);
        item.Property(x=>x.RequestedQuantity).HasColumnType("decimal(12,3)");
        item.Property(x=>x.QuantityUnit).HasMaxLength(20).IsUnicode(false);
        item.Property(x=>x.SpecialRequirements).HasMaxLength(800);
        item.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        item.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        item.Property(x=>x.RowVersion).IsRowVersion();
        item.HasOne<BloodRequest>().WithMany().HasForeignKey(x=>x.BloodRequestId).OnDelete(DeleteBehavior.NoAction);

        var clinical=model.Entity<RequestClinicalData>();
        clinical.ToTable("RequestClinicalData","clinical",t=>t.ExcludeFromMigrations());
        clinical.HasKey(x=>x.RequestClinicalDataId);
        clinical.Property(x=>x.Hemoglobin).HasColumnType("decimal(5,2)");
        clinical.Property(x=>x.Hematocrit).HasColumnType("decimal(5,2)");
        clinical.Property(x=>x.PregnancyHistory).HasMaxLength(500);
        clinical.Property(x=>x.PremedicationDetails).HasMaxLength(1000);
        clinical.Property(x=>x.EmergencyReason).HasMaxLength(1200);
        clinical.Property(x=>x.AdditionalDataJson).HasColumnType("nvarchar(max)");
        clinical.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        clinical.Property(x=>x.RowVersion).IsRowVersion();
        clinical.HasOne<BloodRequest>().WithMany().HasForeignKey(x=>x.BloodRequestId).OnDelete(DeleteBehavior.NoAction);

        var approval=model.Entity<RequestApproval>();
        approval.ToTable("RequestApproval","clinical",t=>t.ExcludeFromMigrations());
        approval.HasKey(x=>x.RequestApprovalId);
        approval.Property(x=>x.ApprovalStage).HasMaxLength(35).IsUnicode(false);
        approval.Property(x=>x.Decision).HasMaxLength(20).IsUnicode(false);
        approval.Property(x=>x.Reason).HasMaxLength(1500);
        approval.Property(x=>x.DecidedAt).HasColumnType("datetime2(3)");
        approval.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        approval.Property(x=>x.RowVersion).IsRowVersion();
        approval.HasOne<BloodRequest>().WithMany().HasForeignKey(x=>x.BloodRequestId).OnDelete(DeleteBehavior.NoAction);
        approval.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReviewerUserId).OnDelete(DeleteBehavior.NoAction);

        var task=model.Entity<ClinicalTask>();
        task.ToTable("ClinicalTask","clinical",t=>t.ExcludeFromMigrations());
        task.HasKey(x=>x.ClinicalTaskId);
        task.Property(x=>x.TaskType).HasMaxLength(70).IsUnicode(false);
        task.Property(x=>x.Priority).HasMaxLength(20).IsUnicode(false);
        task.Property(x=>x.Status).HasMaxLength(20).IsUnicode(false);
        task.Property(x=>x.Summary).HasMaxLength(600);
        task.Property(x=>x.DueAt).HasColumnType("datetime2(3)");
        task.Property(x=>x.CompletedAt).HasColumnType("datetime2(3)");
        task.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        task.Property(x=>x.RowVersion).IsRowVersion();
        task.HasOne<BloodRequest>().WithMany().HasForeignKey(x=>x.BloodRequestId).OnDelete(DeleteBehavior.NoAction);
        task.HasOne<Encounter>().WithMany().HasForeignKey(x=>x.EncounterId).OnDelete(DeleteBehavior.NoAction);
        task.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
