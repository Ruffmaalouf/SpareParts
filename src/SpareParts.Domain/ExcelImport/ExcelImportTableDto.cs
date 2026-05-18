namespace SpareParts.Domain.ExcelImport;

public sealed class ExcelImportTableDto
{
    public string Key { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ColumnCount { get; set; }
    public List<ExcelImportColumnDto> Columns { get; set; } = new();
}
