using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportProfile
    {
        public string TargetKey { get; set; } = string.Empty;
        public Dictionary<string, string> Mappings { get; set; } = new();
    }
}
