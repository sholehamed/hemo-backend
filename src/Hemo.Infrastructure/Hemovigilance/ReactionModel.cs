using Hemo.Domain.Clinical;
using Hemo.Domain.Hemovigilance;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Hemovigilance;

public static class ReactionModel
{
    public static void MapReactions(this ModelBuilder model)
    {
        var e=model.Entity<AdverseEvent>();
        e.ToTable("AdverseEvent","hv",t=>t.ExcludeFromMigrations());
        e.HasKey(x=>x.AdverseEventId);
        e.Property(x=>x.EventNo).HasMaxLength(70);
        e.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        e.Property(x=>x.SymptomsSummary).HasMaxLength(1800);
        e.Property(x=>x.ImmediateActions).HasMaxLength(1800);
        e.Property(x=>x.Outcome).HasMaxLength(1000);
        e.Property(x=>x.Narrative).HasColumnType("nvarchar(max)");
        e.Property(x=>x.DetectedAt).HasColumnType("datetime2(3)");
        e.Property(x=>x.ReportedAt).HasColumnType("datetime2(3)");
        e.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        e.Property(x=>x.RowVersion).IsRowVersion();
        e.HasOne<Transfusion>().WithMany().HasForeignKey(x=>x.TransfusionId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReportedByUserId).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x=>x.EventNo).IsUnique();

        var symptom=model.Entity<EventSymptom>();
        symptom.ToTable("EventSymptom","hv",t=>t.ExcludeFromMigrations());
        symptom.HasKey(x=>x.EventSymptomId);
        symptom.Property(x=>x.SymptomCode).HasMaxLength(80).IsUnicode(false);
        symptom.Property(x=>x.Details).HasMaxLength(450);
        symptom.Property(x=>x.RowVersion).IsRowVersion();
        symptom.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        symptom.HasOne<AdverseEvent>().WithMany().HasForeignKey(x=>x.AdverseEventId).OnDelete(DeleteBehavior.NoAction);

        var physician=model.Entity<PhysicianEventAssessment>();
        physician.ToTable("PhysicianEventAssessment","hv",t=>t.ExcludeFromMigrations());
        physician.HasKey(x=>x.PhysicianEventAssessmentId);
        physician.Property(x=>x.DiagnosisCode).HasMaxLength(80);
        physician.Property(x=>x.DiagnosisText).HasMaxLength(1500);
        physician.Property(x=>x.Causality).HasMaxLength(35).IsUnicode(false);
        physician.Property(x=>x.IsbtCode).HasMaxLength(50);
        physician.Property(x=>x.Interventions).HasMaxLength(1400);
        physician.Property(x=>x.Recommendations).HasMaxLength(1400);
        physician.Property(x=>x.Outcome).HasMaxLength(1400);
        physician.Property(x=>x.AssessedAt).HasColumnType("datetime2(3)");
        physician.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        physician.Property(x=>x.RowVersion).IsRowVersion();
        physician.HasOne<AdverseEvent>().WithMany().HasForeignKey(x=>x.AdverseEventId).OnDelete(DeleteBehavior.NoAction);
        physician.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.AssessedByUserId).OnDelete(DeleteBehavior.NoAction);

        var inv=model.Entity<EventInvestigation>();
        inv.ToTable("EventInvestigation","hv",t=>t.ExcludeFromMigrations());
        inv.HasKey(x=>x.EventInvestigationId);
        inv.Property(x=>x.InvestigationType).HasMaxLength(50).IsUnicode(false);
        inv.Property(x=>x.ResultText).HasMaxLength(2000);
        inv.Property(x=>x.ResultJson).HasColumnType("nvarchar(max)");
        inv.Property(x=>x.InvestigationAt).HasColumnType("datetime2(3)");
        inv.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        inv.Property(x=>x.RowVersion).IsRowVersion();
        inv.HasOne<AdverseEvent>().WithMany().HasForeignKey(x=>x.AdverseEventId).OnDelete(DeleteBehavior.NoAction);
        inv.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.PerformedByUserId).OnDelete(DeleteBehavior.NoAction);

        var rca=model.Entity<RcaCase>();
        rca.ToTable("RcaCase","hv",t=>t.ExcludeFromMigrations());
        rca.HasKey(x=>x.RcaCaseId);
        rca.Property(x=>x.CaseNo).HasMaxLength(70);
        rca.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        rca.Property(x=>x.RootCauseSummary).HasColumnType("nvarchar(max)");
        rca.Property(x=>x.OpenedAt).HasColumnType("datetime2(3)");
        rca.Property(x=>x.ClosedAt).HasColumnType("datetime2(3)");
        rca.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        rca.Property(x=>x.RowVersion).IsRowVersion();
        rca.HasOne<AdverseEvent>().WithMany().HasForeignKey(x=>x.AdverseEventId).OnDelete(DeleteBehavior.NoAction);
        rca.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.OpenedByUserId).OnDelete(DeleteBehavior.NoAction);
        rca.HasIndex(x=>x.CaseNo).IsUnique();

        var cause=model.Entity<RcaCause>();
        cause.ToTable("RcaCause","hv",t=>t.ExcludeFromMigrations());
        cause.HasKey(x=>x.RcaCauseId);
        cause.Property(x=>x.CauseType).HasMaxLength(50).IsUnicode(false);
        cause.Property(x=>x.Description).HasMaxLength(1200);
        cause.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        cause.Property(x=>x.RowVersion).IsRowVersion();
        cause.HasOne<RcaCase>().WithMany().HasForeignKey(x=>x.RcaCaseId).OnDelete(DeleteBehavior.NoAction);

        var action=model.Entity<CorrectiveAction>();
        action.ToTable("CorrectiveAction","hv",t=>t.ExcludeFromMigrations());
        action.HasKey(x=>x.CorrectiveActionId);
        action.Property(x=>x.Title).HasMaxLength(250);
        action.Property(x=>x.Description).HasMaxLength(2000);
        action.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        action.Property(x=>x.DueAt).HasColumnType("datetime2(3)");
        action.Property(x=>x.CompletedAt).HasColumnType("datetime2(3)");
        action.Property(x=>x.VerifiedAt).HasColumnType("datetime2(3)");
        action.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        action.Property(x=>x.RowVersion).IsRowVersion();
        action.HasOne<RcaCase>().WithMany().HasForeignKey(x=>x.RcaCaseId).OnDelete(DeleteBehavior.NoAction);
        action.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.AssignedToUserId).OnDelete(DeleteBehavior.NoAction);
        action.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.VerifiedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
