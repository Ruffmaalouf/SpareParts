using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportFieldDefinition
    {
        public required string Key { get; init; }
        public required string ColumnName { get; init; }
        public required string Description { get; init; }
        public required ExcelImportDataType DataType { get; init; }
        public bool IsRequired { get; init; }
        public IReadOnlyList<string> Aliases { get; init; } = Array.Empty<string>();

        public string DataTypeLabel =>
            DataType switch
            {
                ExcelImportDataType.Integer => "Integer",
                ExcelImportDataType.Decimal => "Decimal",
                ExcelImportDataType.Boolean => "Boolean",
                ExcelImportDataType.Currency => "Currency",
                ExcelImportDataType.Lookup => "Lookup",
                ExcelImportDataType.Enum => "Option",
                ExcelImportDataType.Date => "Date",
                ExcelImportDataType.Identifier => "Identifier",
                _ => "Text"
            };

        public string RequirementLabel => IsRequired ? "Required" : "Optional";
    }
}
