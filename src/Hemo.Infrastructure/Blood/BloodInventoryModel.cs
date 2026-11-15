using Hemo.Domain.Blood;
using Hemo.Domain.Clinical;
using Hemo.Domain.Organization;
using Hemo.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Blood;

public static class BloodInventoryModel
{
    public static void MapBloodInventory(this ModelBuilder model)
    {
        var groups = model.Entity<BloodGroup>();
        groups.ToTable("BloodGroup","blood",t=>t.ExcludeFromMigrations());
        groups.HasKey(x=>x.BloodGroupId);
        groups.Property(x=>x.Code).HasMaxLength(5).IsUnicode(false);
        groups.Property(x=>x.Abo).HasMaxLength(2).IsUnicode(false);
        groups.Property(x=>x.RhD).HasMaxLength(1).IsUnicode(false);
        groups.Property(x=>x.RowVersion).IsRowVersion();

        var unit = model.Entity<BloodUnit>();
        unit.ToTable("BloodUnit","blood",t=>t.ExcludeFromMigrations());
        unit.HasKey(x=>x.BloodUnitId);
        unit.Property(x=>x.UnitCode).HasMaxLength(80);
        unit.Property(x=>x.Status).HasMaxLength(30).IsUnicode(false);
        unit.Property(x=>x.StorageLocation).HasMaxLength(120);
        unit.Property(x=>x.SpecialAttributesJson).HasColumnType("nvarchar(max)");
        unit.Property(x=>x.CollectionDate).HasColumnType("date");
        unit.Property(x=>x.ExpiryDate).HasColumnType("date");
        unit.Property(x=>x.ReceivedAt).HasColumnType("datetime2(3)");
        unit.Property(x=>x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        unit.Property(x=>x.RowVersion).IsRowVersion();
        unit.HasOne<BloodProductType>().WithMany().HasForeignKey(x=>x.ProductTypeId).OnDelete(DeleteBehavior.NoAction);
        unit.HasOne<BloodGroup>().WithMany().HasForeignKey(x=>x.BloodGroupId).OnDelete(DeleteBehavior.NoAction);
        unit.HasOne<Facility>().WithMany().HasForeignKey(x=>x.CurrentFacilityId).OnDelete(DeleteBehavior.NoAction);
        unit.HasIndex(x=>x.UnitCode).IsUnique();

        var reservation=model.Entity<UnitReservation>();
        reservation.ToTable("UnitReservation","blood",t=>t.ExcludeFromMigrations());
        reservation.HasKey(x=>x.UnitReservationId);
        reservation.Property(x=>x.Status).HasMaxLength(20).IsUnicode(false);
        reservation.Property(x=>x.ReservedAt).HasColumnType("datetime2(3)");
        reservation.Property(x=>x.ReleasedAt).HasColumnType("datetime2(3)");
        reservation.Property(x=>x.ReleaseReason).HasMaxLength(500);
        reservation.Property(x=>x.RowVersion).IsRowVersion();
        reservation.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        reservation.HasOne<BloodRequestItem>().WithMany().HasForeignKey(x=>x.BloodRequestItemId).OnDelete(DeleteBehavior.NoAction);
        reservation.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ReservedByUserId).OnDelete(DeleteBehavior.NoAction);

        var cross=model.Entity<Crossmatch>();
        cross.ToTable("Crossmatch","blood",t=>t.ExcludeFromMigrations());
        cross.HasKey(x=>x.CrossmatchId);
        cross.Property(x=>x.CompatibilityResult).HasMaxLength(30).IsUnicode(false);
        cross.Property(x=>x.Status).HasMaxLength(25).IsUnicode(false);
        cross.Property(x=>x.AntibodyScreenResult).HasMaxLength(30).IsUnicode(false);
        cross.Property(x=>x.Comments).HasMaxLength(1500);
        cross.Property(x=>x.PerformedAt).HasColumnType("datetime2(3)");
        cross.Property(x=>x.ApprovedAt).HasColumnType("datetime2(3)");
        cross.Property(x=>x.RowVersion).IsRowVersion();
        cross.HasOne<BloodRequestItem>().WithMany().HasForeignKey(x=>x.BloodRequestItemId).OnDelete(DeleteBehavior.NoAction);
        cross.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
        cross.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.PerformedByUserId).OnDelete(DeleteBehavior.NoAction);
        cross.HasOne<AccountUser>().WithMany().HasForeignKey(x=>x.ApprovedByUserId).OnDelete(DeleteBehavior.NoAction);

        var movement=model.Entity<InventoryMovement>();
        movement.ToTable("InventoryMovement","blood",t=>t.ExcludeFromMigrations());
        movement.HasKey(x=>x.InventoryMovementId);
        movement.Property(x=>x.MovementType).HasMaxLength(50).IsUnicode(false);
        movement.Property(x=>x.ReferenceType).HasMaxLength(45).IsUnicode(false);
        movement.Property(x=>x.PriorStatus).HasMaxLength(30).IsUnicode(false);
        movement.Property(x=>x.NewStatus).HasMaxLength(30).IsUnicode(false);
        movement.Property(x=>x.Explanation).HasMaxLength(1200);
        movement.Property(x=>x.MovementAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        movement.HasOne<BloodUnit>().WithMany().HasForeignKey(x=>x.BloodUnitId).OnDelete(DeleteBehavior.NoAction);
    }
}
