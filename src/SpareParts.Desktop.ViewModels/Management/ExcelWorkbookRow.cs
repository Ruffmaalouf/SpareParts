using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelWorkbookRow
    {
        public int RowNumber { get; init; }
        public IReadOnlyDictionary<string, string> Cells { get; init; } = new Dictionary<string, string>();
    }
}
