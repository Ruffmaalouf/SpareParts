using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace SpareParts.Desktop.Wpf.Helpers
{
    public static class JsonGridHelper
    {
        public static DataTable ToDataTable(IReadOnlyList<JsonElement> rows)
        {
            var table = new DataTable();
            var columns = new List<string>();

            foreach (var row in rows)
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                foreach (var property in row.EnumerateObject())
                {
                    if (!columns.Contains(property.Name)) columns.Add(property.Name);
                }
            }

            foreach (var column in columns) table.Columns.Add(column, typeof(string));

            foreach (var row in rows)
            {
                if (row.ValueKind != JsonValueKind.Object) continue;
                var dataRow = table.NewRow();
                foreach (var column in columns)
                {
                    dataRow[column] = row.TryGetProperty(column, out var value) ? FormatValue(value) : string.Empty;
                }
                table.Rows.Add(dataRow);
            }

            return table;
        }

        private static string FormatValue(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "Yes",
            JsonValueKind.False => "No",
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => value.ToString()
        };
    }
}
