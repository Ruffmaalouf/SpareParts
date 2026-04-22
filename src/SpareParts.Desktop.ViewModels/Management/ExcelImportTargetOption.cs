namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportTargetOption
    {
        public required string Key { get; init; }
        public required string Name { get; init; }
        public required string TableName { get; init; }
        public required string Description { get; init; }
        public int ColumnCount { get; init; }

        public string Summary => $"{TableName} · {ColumnCount} column(s)";
    }
}
