using Hemo.Domain.Blood;
using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Blood;

public static class BloodCatalogModel
{
    public static void MapBloodCatalog(this ModelBuilder builder)
    {
        var product=builder.Entity<BloodProductType>();
        product.ToTable("ProductType","blood",t=>t.ExcludeFromMigrations());
        product.HasKey(x=>x.ProductTypeId);
        product.Property(x=>x.Code).HasMaxLength(40).IsUnicode(false);
        product.Property(x=>x.Name).HasMaxLength(180);
        product.Property(x=>x.StorageMinC).HasColumnType("decimal(5,2)");
        product.Property(x=>x.StorageMaxC).HasColumnType("decimal(5,2)");
        product.Property(x=>x.IsActive).HasDefaultValue(true);
        product.Property(x=>x.CreatedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        product.Property(x=>x.RowVersion).IsRowVersion();
        product.HasIndex(x=>x.Code).IsUnique();
    }
}
