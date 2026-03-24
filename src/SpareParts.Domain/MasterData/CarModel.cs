using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class CarModel : AuditableEntity
    {
        public int CarBrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Year { get; set; }
        public string? EngineType { get; set; }
        public decimal BasePrice { get; set; }
        public string? ImagePath { get; set; }
        public byte[]? ImageData { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
