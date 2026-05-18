using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class Warehouse : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Address { get; set; }
        public bool IsMain { get; set; }
    }
}
