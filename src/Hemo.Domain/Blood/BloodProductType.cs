namespace Hemo.Domain.Blood;
public sealed class BloodProductType
{
    public long ProductTypeId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal? StorageMinC { get; set; }
    public decimal? StorageMaxC { get; set; }
    public int? ShelfLifeDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
