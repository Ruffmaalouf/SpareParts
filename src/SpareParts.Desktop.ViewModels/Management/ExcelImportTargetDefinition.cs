using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    internal sealed class ExcelImportTargetDefinition
    {
        public required string Key { get; init; }
        public required string Name { get; init; }
        public required string TableName { get; init; }
        public required string Description { get; init; }
        public required string Endpoint { get; init; }
        public required IReadOnlyList<ExcelImportFieldDefinition> Fields { get; init; }
        public required Func<IReadOnlyDictionary<string, string?>, ExcelImportExecutionContext, int, object> BuildPayload { get; init; }
    }
}
