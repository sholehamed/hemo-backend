using Hemo.Domain.Blood;
using Hemo.Domain.Clinical;
using Hemo.Domain.Organization;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Blood;

public static class BloodLogisticsModel
{
    public static void MapBloodLogistics(this ModelBuilder model)
    {
        var issue=model.Entity<UnitIssue>();
        issue.ToTable("UnitIssue","blood",t=>t.ExcludeFromMigrations());
        issue.HasKey(x=>x.UnitIssueId);
        issue.Property(x=>x.IssueNo).HasMaxLength(75);
        issue.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        issue.Property(x=>x.TransportConditionNote).HasMaxLength(1000);
        issue.Property(x=>x.IssuedAt).HasColumnType("datetime2(3)");
        issue.Property(x=>x.ReceivedAt).HasColumnType("datetime2(3)");
        issue.Property(x=>x.RowVersion).IsRowVersion();
        issue.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        issue.HasOne<BloodRequestItem>().WithMany().HasForeignKey(x=>x.BloodRequestItemId).OnDelete(DeleteBehavior.NoAction);
        issue.HasOne<Department>().WithMany().HasForeignKey(x=>x.IssuedToDepartmentId).OnDelete(DeleteBehavior.NoAction);
        issue.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.IssuedByUserId).OnDelete(DeleteBehavior.NoAction);
        issue.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReceivedByUserId).OnDelete(DeleteBehavior.NoAction);
        issue.HasIndex(x=>x.IssueNo).IsUnique();

        var dispatch=model.Entity<Dispatch>();
        dispatch.ToTable("Dispatch","blood",t=>t.ExcludeFromMigrations());
        dispatch.HasKey(x=>x.DispatchId);
        dispatch.Property(x=>x.DispatchNo).HasMaxLength(75);
        dispatch.Property(x=>x.Status).HasMaxLength(30).IsUnicode(false);
        dispatch.Property(x=>x.ContainerType).HasMaxLength(80);
        dispatch.Property(x=>x.TemperatureAtExitC).HasColumnType("decimal(5,2)");
        dispatch.Property(x=>x.Note).HasMaxLength(1200);
        dispatch.Property(x=>x.CreatedAtDispatch).HasColumnType("datetime2(3)");
        dispatch.Property(x=>x.SentAt).HasColumnType("datetime2(3)");
        dispatch.Property(x=>x.DeliveredAt).HasColumnType("datetime2(3)");
        dispatch.Property(x=>x.RowVersion).IsRowVersion();
        dispatch.HasOne<Facility>().WithMany().HasForeignKey(x=>x.FacilityId).OnDelete(DeleteBehavior.NoAction);
        dispatch.HasOne<Department>().WithMany().HasForeignKey(x=>x.DepartmentId).OnDelete(DeleteBehavior.NoAction);
        dispatch.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        dispatch.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReceiverUserId).OnDelete(DeleteBehavior.NoAction);
        dispatch.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.CourierUserId).OnDelete(DeleteBehavior.NoAction);
        dispatch.HasIndex(x=>x.DispatchNo).IsUnique();

        var di=model.Entity<DispatchItem>();
        di.ToTable("DispatchItem","blood",t=>t.ExcludeFromMigrations());
        di.HasKey(x=>x.DispatchItemId);
        di.Property(x=>x.TemperatureAtHandoverC).HasColumnType("decimal(5,2)");
        di.Property(x=>x.ConditionNote).HasMaxLength(700);
        di.Property(x=>x.RowVersion).IsRowVersion();
        di.HasOne<Dispatch>().WithMany().HasForeignKey(x=>x.DispatchId).OnDelete(DeleteBehavior.NoAction);
        di.HasOne<UnitIssue>().WithMany().HasForeignKey(x=>x.UnitIssueId).OnDelete(DeleteBehavior.NoAction);

        var ret=model.Entity<UnitReturn>();
        ret.ToTable("UnitReturn","blood",t=>t.ExcludeFromMigrations());
        ret.HasKey(x=>x.UnitReturnId);
        ret.Property(x=>x.ReasonCode).HasMaxLength(60).IsUnicode(false);
        ret.Property(x=>x.ConditionCode).HasMaxLength(40).IsUnicode(false);
        ret.Property(x=>x.Decision).HasMaxLength(20).IsUnicode(false);
        ret.Property(x=>x.Status).HasMaxLength(20).IsUnicode(false);
        ret.Property(x=>x.TemperatureC).HasColumnType("decimal(5,2)");
        ret.Property(x=>x.Notes).HasMaxLength(1400);
        ret.Property(x=>x.ReturnedAt).HasColumnType("datetime2(3)");
        ret.Property(x=>x.ReviewedAt).HasColumnType("datetime2(3)");
        ret.Property(x=>x.RowVersion).IsRowVersion();
        ret.HasOne<UnitIssue>().WithMany().HasForeignKey(x=>x.UnitIssueId).OnDelete(DeleteBehavior.NoAction);
        ret.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        ret.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReturnedByUserId).OnDelete(DeleteBehavior.NoAction);
        ret.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReceivedByUserId).OnDelete(DeleteBehavior.NoAction);
        ret.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReviewedByUserId).OnDelete(DeleteBehavior.NoAction);

        var waste=model.Entity<UnitWaste>();
        waste.ToTable("UnitWaste","blood",t=>t.ExcludeFromMigrations());
        waste.HasKey(x=>x.UnitWasteId);
        waste.Property(x=>x.WasteCode).HasMaxLength(80);
        waste.Property(x=>x.ReasonCode).HasMaxLength(70).IsUnicode(false);
        waste.Property(x=>x.WasteDate).HasColumnType("datetime2(3)");
        waste.Property(x=>x.ApprovedAt).HasColumnType("datetime2(3)");
        waste.Property(x=>x.DisposalMethod).HasMaxLength(250);
        waste.Property(x=>x.Description).HasMaxLength(1500);
        waste.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        waste.HasOne<UnitReturn>().WithMany().HasForeignKey(x=>x.UnitReturnId).OnDelete(DeleteBehavior.NoAction);
        waste.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.RecordedByUserId).OnDelete(DeleteBehavior.NoAction);
        waste.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ApprovedByUserId).OnDelete(DeleteBehavior.NoAction);
    }
}
