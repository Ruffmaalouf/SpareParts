namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class PartExcelImportRow
    {
        public int RowNumber { get; init; }
        public string? InternalCode { get; init; }
        public string? Barcode { get; init; }
        public string? Name { get; init; }
        public string? OEMNumber { get; init; }
        public string? Category { get; init; }
        public string? Brand { get; init; }
        public string? CostPrice { get; init; }
        public string? SalePrice { get; init; }
        public string? AveragePrice { get; init; }
        public string? Currency { get; init; }
        public string? MinStock { get; init; }
        public string? Notes { get; init; }
        public string? Condition { get; init; }
    }
}
