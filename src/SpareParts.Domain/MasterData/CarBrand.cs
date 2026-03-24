using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class CarBrand : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public string? LogoPath { get; set; }
        public byte[]? LogoData { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
