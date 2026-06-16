using SpareParts.Domain.Common;

namespace SpareParts.Domain.Compatibility
{
    public class PartCompatibility : AuditableEntity
    {
        public int TenantId { get; set; }
        public int PartId { get; set; }
        public string? CompatibleMakes { get; set; }
        public string? CompatibleModels { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public string? CompatibleEngines { get; set; }
        public string? OemNumbers { get; set; }
        public string? AlternativeNumbers { get; set; }
        public string? Notes { get; set; }
    }
}
