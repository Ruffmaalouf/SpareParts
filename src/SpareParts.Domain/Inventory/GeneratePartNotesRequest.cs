namespace SpareParts.Domain.Inventory
{
    public sealed class GeneratePartNotesRequest
    {
        public string InternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public string? ExistingNotes { get; set; }
    }
}
