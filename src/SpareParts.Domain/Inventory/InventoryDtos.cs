namespace SpareParts.Domain.Inventory
{
    // ── Part brand (spare-part manufacturer brand, not car brand) ─────────────
    public class BrandDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; } = string.Empty;
        public bool   IsActive { get; set; }
    }

    public class CreateBrandRequest
    {
        public string Name     { get; set; } = string.Empty;
        public bool   IsActive { get; set; } = true;
    }

    // ── Category ──────────────────────────────────────────────────────────────
    public class CategoryDto
    {
        public int     Id       { get; set; }
        public string  Name     { get; set; } = string.Empty;
        public int?    ParentId { get; set; }
    }

    public class CreateCategoryRequest
    {
        public string  Name     { get; set; } = string.Empty;
        public int?    ParentId { get; set; }
    }

    // ── Part ──────────────────────────────────────────────────────────────────
    public class PartDto
    {
        public int           Id           { get; set; }
        public string        InternalCode { get; set; } = string.Empty;
        public string?       Barcode      { get; set; }
        public string        Name         { get; set; } = string.Empty;
        public string?       OEMNumber    { get; set; }
        public PartCondition Condition    { get; set; } = PartCondition.New;
        public int           CategoryId   { get; set; }
        public int?          BrandId      { get; set; }
        public decimal       CostPrice    { get; set; }
        public decimal       SalePrice    { get; set; }
        public string        Currency     { get; set; } = "USD";
        public int           MinStock     { get; set; }
        public string?       Notes        { get; set; }
        public bool          IsActive     { get; set; }
    }

    public class CreatePartRequest
    {
        public string        InternalCode { get; set; } = string.Empty;
        public string?       Barcode      { get; set; }
        public string        Name         { get; set; } = string.Empty;
        public string?       OEMNumber    { get; set; }
        public PartCondition Condition    { get; set; } = PartCondition.New;
        public int           CategoryId   { get; set; }
        public int?          BrandId      { get; set; }
        public decimal       CostPrice    { get; set; }
        public decimal       SalePrice    { get; set; }
        public string        Currency     { get; set; } = "USD";
        public int           MinStock     { get; set; }
        public string?       Notes        { get; set; }
    }
}
