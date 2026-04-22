using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelWorkbookSheet
    {
        public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();
        public IReadOnlyList<ExcelWorkbookRow> Rows { get; init; } = Array.Empty<ExcelWorkbookRow>();

        public int RowCount => Rows.Count;
    }
}
