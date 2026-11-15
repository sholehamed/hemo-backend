using Hemo.Domain.Blood;
using Hemo.Domain.Clinical;
using Hemo.Domain.Patients;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Clinical;

public static class TransfusionModel
{
    public static void MapTransfusions(this ModelBuilder model)
    {
        var t=model.Entity<Transfusion>();
        t.ToTable("Transfusion","clinical",b=>b.ExcludeFromMigrations());
        t.HasKey(x=>x.TransfusionId);
        t.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        t.Property(x=>x.StopReason).HasMaxLength(1500);
        t.Property(x=>x.FinalDisposition).HasMaxLength(30).IsUnicode(false);
        t.Property(x=>x.StartedAt).HasColumnType("datetime2(3)");
        t.Property(x=>x.EndedAt).HasColumnType("datetime2(3)");
        t.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        t.Property(x=>x.RowVersion).IsRowVersion();
        t.HasOne<Encounter>().WithMany().HasForeignKey(x=>x.EncounterId).OnDelete(DeleteBehavior.NoAction);
        t.HasOne<BloodRequestItem>().WithMany().HasForeignKey(x=>x.BloodRequestItemId).OnDelete(DeleteBehavior.NoAction);
        t.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        t.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.StartedByUserId).OnDelete(DeleteBehavior.NoAction);
        t.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.EndedByUserId).OnDelete(DeleteBehavior.NoAction);

        var checklist=model.Entity<TransfusionChecklist>();
        checklist.ToTable("TransfusionChecklist","clinical",b=>b.ExcludeFromMigrations());
        checklist.HasKey(x=>x.TransfusionChecklistId);
        checklist.Property(x=>x.CheckedAt).HasColumnType("datetime2(3)");
        checklist.Property(x=>x.Note).HasMaxLength(800);
        checklist.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        checklist.Property(x=>x.RowVersion).IsRowVersion();
        checklist.HasOne<Transfusion>().WithMany().HasForeignKey(x=>x.TransfusionId).OnDelete(DeleteBehavior.NoAction);
        checklist.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.CheckedByUserId).OnDelete(DeleteBehavior.NoAction);

        var vital=model.Entity<VitalObservation>();
        vital.ToTable("VitalObservation","clinical",b=>b.ExcludeFromMigrations());
        vital.HasKey(x=>x.VitalObservationId);
        vital.Property(x=>x.TimepointCode).HasMaxLength(30).IsUnicode(false);
        vital.Property(x=>x.ObservedAt).HasColumnType("datetime2(3)");
        vital.Property(x=>x.TemperatureC).HasColumnType("decimal(4,1)");
        vital.Property(x=>x.UrineOutputMl).HasColumnType("decimal(9,2)");
        vital.Property(x=>x.ClinicalNote).HasMaxLength(1200);
        vital.HasOne<Transfusion>().WithMany().HasForeignKey(x=>x.TransfusionId).OnDelete(DeleteBehavior.NoAction);
        vital.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.RecordedByUserId).OnDelete(DeleteBehavior.NoAction);

        var action=model.Entity<TransfusionAction>();
        action.ToTable("TransfusionAction","clinical",b=>b.ExcludeFromMigrations());
        action.HasKey(x=>x.TransfusionActionId);
        action.Property(x=>x.ActionType).HasMaxLength(60).IsUnicode(false);
        action.Property(x=>x.ActionAt).HasColumnType("datetime2(3)");
        action.Property(x=>x.Description).HasMaxLength(1500);
        action.HasOne<Transfusion>().WithMany().HasForeignKey(x=>x.TransfusionId).OnDelete(DeleteBehavior.NoAction);
        action.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.PerformedByUserId).OnDelete(DeleteBehavior.NoAction);

        var sign=model.Entity<TransfusionSignoff>();
        sign.ToTable("TransfusionSignoff","clinical",b=>b.ExcludeFromMigrations());
        sign.HasKey(x=>x.TransfusionSignoffId);
        sign.Property(x=>x.SignoffRole).HasMaxLength(35).IsUnicode(false);
        sign.Property(x=>x.SignedAt).HasColumnType("datetime2(3)");
        sign.Property(x=>x.SignatureHash).HasColumnType("varbinary(32)");
        sign.Property(x=>x.Comment).HasMaxLength(800);
        sign.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        sign.Property(x=>x.RowVersion).IsRowVersion();
        sign.HasOne<Transfusion>().WithMany().HasForeignKey(x=>x.TransfusionId).OnDelete(DeleteBehavior.NoAction);
        sign.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.SignerUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
