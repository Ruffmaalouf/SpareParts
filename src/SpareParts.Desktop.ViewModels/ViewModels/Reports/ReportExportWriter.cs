using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal static class ReportExportWriter
    {
        private const int PdfMaxColumnsWidth = 112;
        private const int PdfLinesPerPage = 44;

        public static void WriteCsv(DataView view, string path)
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            var columns = GetColumns(view);

            writer.WriteLine(string.Join(",", columns.Select(column => EscapeCsvValue(column.ColumnName))));
            foreach (DataRowView row in view)
            {
                writer.WriteLine(string.Join(",", columns.Select(column => EscapeCsvValue(row.Row[column]))));
            }
        }

        public static void WriteJson(DataView view, string path)
        {
            var rows = new List<Dictionary<string, object?>>();
            var columns = GetColumns(view);

            foreach (DataRowView row in view)
            {
                var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in columns)
                {
                    dictionary[column.ColumnName] = NormalizeExportValue(row.Row[column]);
                }

                rows.Add(dictionary);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            File.WriteAllText(path, JsonSerializer.Serialize(rows, options), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        public static void WriteExcelXml(DataView view, string path, string worksheetName, string title, string summary, DateTime generatedAt)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            };

            using var stream = File.Create(path);
            using var writer = XmlWriter.Create(stream, settings);
            var columns = GetColumns(view);

            writer.WriteStartDocument();
            writer.WriteStartElement("Workbook", "urn:schemas-microsoft-com:office:spreadsheet");
            writer.WriteAttributeString("xmlns", "o", null, "urn:schemas-microsoft-com:office:office");
            writer.WriteAttributeString("xmlns", "x", null, "urn:schemas-microsoft-com:office:excel");
            writer.WriteAttributeString("xmlns", "ss", null, "urn:schemas-microsoft-com:office:spreadsheet");

            writer.WriteStartElement("Styles");
            WriteExcelStyle(writer, "Header", bold: true, interiorColor: "#EFE8DB");
            WriteExcelStyle(writer, "Meta", bold: false, interiorColor: "#F7F2EA");
            WriteExcelStyle(writer, "DefaultText", bold: false, interiorColor: null);
            writer.WriteEndElement();

            writer.WriteStartElement("Worksheet");
            writer.WriteAttributeString("ss", "Name", null, BuildSafeWorksheetName(worksheetName));

            writer.WriteStartElement("Table");
            WriteExcelTextRow(writer, "Meta", title, columns.Count);
            WriteExcelTextRow(writer, "Meta", summary, columns.Count);
            WriteExcelTextRow(writer, "Meta", $"Generated on {generatedAt:yyyy-MM-dd HH:mm:ss}", columns.Count);

            writer.WriteStartElement("Row");
            foreach (var column in columns)
            {
                WriteExcelCell(writer, column.ColumnName, "String", "Header");
            }
            writer.WriteEndElement();

            foreach (DataRowView row in view)
            {
                writer.WriteStartElement("Row");
                foreach (var column in columns)
                {
                    var value = NormalizeExportValue(row.Row[column]);
                    WriteExcelCell(writer, FormatExcelCellValue(value), GetExcelCellType(value), "DefaultText");
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        public static void WritePdf(DataView view, string path, string title, string summary, DateTime generatedAt)
        {
            var lines = BuildPdfLines(view, title, summary, generatedAt);
            var pages = PaginateLines(lines, PdfLinesPerPage);

            var objects = new List<string>();
            var pageObjectNumbers = new List<int>();
            var contentObjectNumbers = new List<int>();

            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                pageObjectNumbers.Add(3 + (pageIndex * 2));
                contentObjectNumbers.Add(4 + (pageIndex * 2));
            }

            var fontObjectNumber = 3 + (pages.Count * 2);

            objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
            objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", pageObjectNumbers.Select(number => $"{number} 0 R"))}] /Count {pages.Count} >>");

            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> /Contents {contentObjectNumbers[pageIndex]} 0 R >>");
                var content = BuildPdfContentStream(pages[pageIndex]);
                objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            }

            objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");

            WritePdfFile(path, objects);
        }

        private static void WriteExcelStyle(XmlWriter writer, string id, bool bold, string? interiorColor)
        {
            writer.WriteStartElement("Style");
            writer.WriteAttributeString("ss", "ID", null, id);

            writer.WriteStartElement("Font");
            writer.WriteAttributeString("ss", "Bold", null, bold ? "1" : "0");
            writer.WriteEndElement();

            if (!string.IsNullOrWhiteSpace(interiorColor))
            {
                writer.WriteStartElement("Interior");
                writer.WriteAttributeString("ss", "Color", null, interiorColor);
                writer.WriteAttributeString("ss", "Pattern", null, "Solid");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteExcelTextRow(XmlWriter writer, string styleId, string text, int columnCount)
        {
            writer.WriteStartElement("Row");
            WriteExcelCell(writer, text, "String", styleId, mergeAcross: Math.Max(0, columnCount - 1));
            writer.WriteEndElement();
        }

        private static void WriteExcelCell(XmlWriter writer, string value, string type, string styleId, int mergeAcross = 0)
        {
            writer.WriteStartElement("Cell");
            writer.WriteAttributeString("ss", "StyleID", null, styleId);
            if (mergeAcross > 0)
            {
                writer.WriteAttributeString("ss", "MergeAcross", null, mergeAcross.ToString(CultureInfo.InvariantCulture));
            }

            writer.WriteStartElement("Data");
            writer.WriteAttributeString("ss", "Type", null, type);
            writer.WriteString(value);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static string GetExcelCellType(object? value)
        {
            return value switch
            {
                null => "String",
                bool => "Boolean",
                byte or short or int or long or float or double or decimal => "Number",
                DateTime => "DateTime",
                _ => "String"
            };
        }

        private static string FormatExcelCellValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                bool boolean => boolean ? "1" : "0",
                DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        private static IReadOnlyList<string> BuildPdfLines(DataView view, string title, string summary, DateTime generatedAt)
        {
            var columns = GetColumns(view);
            var widths = CalculatePdfColumnWidths(view, columns);
            var delimiter = " | ";
            var lines = new List<string>
            {
                title,
                summary,
                $"Generated on {generatedAt:yyyy-MM-dd HH:mm:ss}",
                string.Empty
            };

            var header = string.Join(delimiter, columns.Select((column, index) => PadPdfValue(column.ColumnName, widths[index])));
            lines.Add(header);
            lines.Add(new string('-', Math.Min(PdfMaxColumnsWidth, header.Length)));

            foreach (DataRowView row in view)
            {
                var values = columns
                    .Select((column, index) => PadPdfValue(FormatExportValue(NormalizeExportValue(row.Row[column])), widths[index]))
                    .ToArray();
                lines.Add(string.Join(delimiter, values));
            }

            if (view.Count == 0)
            {
                lines.Add("No rows returned for the selected filters.");
            }

            return lines;
        }

        private static int[] CalculatePdfColumnWidths(DataView view, IReadOnlyList<DataColumn> columns)
        {
            var widths = columns
                .Select(column => Math.Max(8, Math.Min(24, column.ColumnName.Length)))
                .ToArray();

            foreach (DataRowView row in view)
            {
                for (var index = 0; index < columns.Count; index++)
                {
                    var value = FormatExportValue(NormalizeExportValue(row.Row[columns[index]]));
                    widths[index] = Math.Max(widths[index], Math.Min(24, value.Length));
                }
            }

            var delimiterWidth = Math.Max(0, columns.Count - 1) * 3;
            while (widths.Sum() + delimiterWidth > PdfMaxColumnsWidth)
            {
                var widestIndex = Array.IndexOf(widths, widths.Max());
                if (widestIndex < 0 || widths[widestIndex] <= 8)
                {
                    break;
                }

                widths[widestIndex]--;
            }

            return widths;
        }

        private static List<List<string>> PaginateLines(IReadOnlyList<string> lines, int linesPerPage)
        {
            var pages = new List<List<string>>();
            var current = new List<string>();

            foreach (var line in lines.SelectMany(line => WrapPdfLine(line, PdfMaxColumnsWidth)))
            {
                if (current.Count == linesPerPage)
                {
                    pages.Add(current);
                    current = new List<string>();
                }

                current.Add(line);
            }

            if (current.Count > 0)
            {
                pages.Add(current);
            }

            return pages.Count == 0 ? new List<List<string>> { new List<string> { "No report data." } } : pages;
        }

        private static IEnumerable<string> WrapPdfLine(string line, int maxLength)
        {
            var safeLine = line ?? string.Empty;
            if (safeLine.Length <= maxLength)
            {
                yield return safeLine;
                yield break;
            }

            var remaining = safeLine;
            while (remaining.Length > maxLength)
            {
                yield return remaining[..maxLength];
                remaining = remaining[maxLength..];
            }

            if (remaining.Length > 0)
            {
                yield return remaining;
            }
        }

        private static string BuildPdfContentStream(IEnumerable<string> lines)
        {
            var builder = new StringBuilder();
            builder.AppendLine("BT");
            builder.AppendLine("/F1 8.5 Tf");
            builder.AppendLine("10 TL");
            builder.AppendLine("30 565 Td");

            var isFirstLine = true;
            foreach (var line in lines)
            {
                if (!isFirstLine)
                {
                    builder.AppendLine("T*");
                }

                builder.Append('(');
                builder.Append(EscapePdfText(line));
                builder.AppendLine(") Tj");
                isFirstLine = false;
            }

            builder.Append("ET");
            return builder.ToString();
        }

        private static void WritePdfFile(string path, IReadOnlyList<string> objects)
        {
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);

            writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
            var offsets = new List<long> { 0 };

            for (var index = 0; index < objects.Count; index++)
            {
                offsets.Add(stream.Position);
                writer.Write(Encoding.ASCII.GetBytes($"{index + 1} 0 obj\n"));
                writer.Write(Encoding.ASCII.GetBytes(objects[index]));
                writer.Write(Encoding.ASCII.GetBytes("\nendobj\n"));
            }

            var crossReferenceStart = stream.Position;
            writer.Write(Encoding.ASCII.GetBytes($"xref\n0 {objects.Count + 1}\n"));
            writer.Write(Encoding.ASCII.GetBytes("0000000000 65535 f \n"));
            foreach (var offset in offsets.Skip(1))
            {
                writer.Write(Encoding.ASCII.GetBytes($"{offset:D10} 00000 n \n"));
            }

            writer.Write(Encoding.ASCII.GetBytes($"trailer << /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{crossReferenceStart}\n%%EOF"));
        }

        private static IReadOnlyList<DataColumn> GetColumns(DataView view)
            => (view.Table ?? throw new InvalidOperationException("Report export requires a table-backed data view."))
                .Columns
                .Cast<DataColumn>()
                .ToList();

        private static string EscapeCsvValue(object? value)
        {
            var text = FormatExportValue(NormalizeExportValue(value));
            var escaped = text.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static object? NormalizeExportValue(object? value)
            => value == DBNull.Value ? null : value;

        private static string FormatExportValue(object? value)
        {
            return value switch
            {
                null => string.Empty,
                DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                bool boolean => boolean ? "True" : "False",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
                _ => value.ToString() ?? string.Empty
            };
        }

        private static string BuildSafeWorksheetName(string worksheetName)
        {
            var safe = string.IsNullOrWhiteSpace(worksheetName) ? "Report" : worksheetName.Trim();
            foreach (var invalidCharacter in new[] { '\\', '/', '?', '*', '[', ']', ':' })
            {
                safe = safe.Replace(invalidCharacter, '-');
            }

            return safe.Length > 31 ? safe[..31] : safe;
        }

        private static string PadPdfValue(string value, int width)
        {
            var text = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
            if (text.Length > width)
            {
                return width <= 3 ? text[..width] : $"{text[..(width - 3)]}...";
            }

            return text.PadRight(width);
        }

        private static string EscapePdfText(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (character is '(' or ')' or '\\')
                {
                    builder.Append('\\');
                    builder.Append(character);
                }
                else if (character is >= ' ' and <= '~')
                {
                    builder.Append(character);
                }
                else
                {
                    builder.Append('?');
                }
            }

            return builder.ToString();
        }
    }
}
