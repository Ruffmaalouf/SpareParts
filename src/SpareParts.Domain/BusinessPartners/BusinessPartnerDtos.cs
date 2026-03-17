namespace SpareParts.Domain.BusinessPartners
{
    // ── Customer ──────────────────────────────────────────────────────────────
    public class CustomerDto
    {
        public int      Id             { get; set; }
        public string   Name           { get; set; } = string.Empty;
        public string?  Phone          { get; set; }
        public string?  Email          { get; set; }
        public string?  Address        { get; set; }
        public string?  TaxNumber      { get; set; }
        public decimal  OpeningBalance { get; set; }
    }

    public class CreateCustomerRequest
    {
        public string   Name           { get; set; } = string.Empty;
        public string?  Phone          { get; set; }
        public string?  Email          { get; set; }
        public string?  Address        { get; set; }
        public string?  TaxNumber      { get; set; }
        public decimal  OpeningBalance { get; set; }
    }

    // ── Supplier ──────────────────────────────────────────────────────────────
    public class SupplierDto
    {
        public int      Id             { get; set; }
        public string   Name           { get; set; } = string.Empty;
        public string?  Phone          { get; set; }
        public string?  Email          { get; set; }
        public string?  Address        { get; set; }
        public string?  TaxNumber      { get; set; }
        public decimal  OpeningBalance { get; set; }
    }

    public class CreateSupplierRequest
    {
        public string   Name           { get; set; } = string.Empty;
        public string?  Phone          { get; set; }
        public string?  Email          { get; set; }
        public string?  Address        { get; set; }
        public string?  TaxNumber      { get; set; }
        public decimal  OpeningBalance { get; set; }
    }
}
