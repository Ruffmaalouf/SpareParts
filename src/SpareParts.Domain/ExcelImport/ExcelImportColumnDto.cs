namespace SpareParts.Domain.ExcelImport;

public sealed class ExcelImportColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public bool HasDefaultValue { get; set; }
    public int? MaxLength { get; set; }
    public int Precision { get; set; }
    public int Scale { get; set; }
    public int OrdinalPosition { get; set; }
    public string? ReferencedSchemaName { get; set; }
    public string? ReferencedTableName { get; set; }
    public string? ReferencedColumnName { get; set; }
    public string Description { get; set; } = string.Empty;
}
