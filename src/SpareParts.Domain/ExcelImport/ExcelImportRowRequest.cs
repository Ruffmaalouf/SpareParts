namespace SpareParts.Domain.ExcelImport;

public sealed class ExcelImportRowRequest
{
    public string TableKey { get; set; } = string.Empty;
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
