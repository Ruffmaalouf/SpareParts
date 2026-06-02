using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class CarModelEntity : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string EngineType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string? ImagePath { get; set; }
        public int? BrandId { get; set; }
    }
}
