using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelWorkbookReader
    {
        public ExcelWorkbookSheet Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new DomainValidationException("Select an Excel workbook first.", "excel_import_file_required");
            }

            if (!File.Exists(filePath))
            {
                throw new DomainValidationException("The selected Excel file was not found.", "excel_import_file_missing");
            }

            if (!string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("Only .xlsx Excel workbooks are supported.", "excel_import_extension_invalid");
            }

            using var archive = ZipFile.OpenRead(filePath);
            var worksheet = LoadFirstWorksheet(archive);
            var sharedStrings = LoadSharedStrings(archive);

            var spreadsheetNs = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            var rows = worksheet.Root?
                .Element(spreadsheetNs + "sheetData")?
                .Elements(spreadsheetNs + "row")
                .ToList()
                ?? new List<XElement>();

            if (rows.Count == 0)
            {
                throw new DomainValidationException("The selected workbook is empty.", "excel_import_empty_workbook");
            }

            var headerCells = ReadRowCells(rows[0], sharedStrings, spreadsheetNs);
            var headersByColumn = BuildHeaders(headerCells);
            if (headersByColumn.Count == 0)
            {
                throw new DomainValidationException("The workbook must contain a header row.", "excel_import_missing_headers");
            }

            var dataRows = new List<ExcelWorkbookRow>();

            foreach (var row in rows.Skip(1))
            {
                var cellValues = ReadRowCells(row, sharedStrings, spreadsheetNs);
                if (cellValues.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (columnIndex, headerName) in headersByColumn)
                {
                    cellValues.TryGetValue(columnIndex, out var rawValue);
                    cells[headerName] = string.IsNullOrWhiteSpace(rawValue) ? string.Empty : rawValue.Trim();
                }

                dataRows.Add(new ExcelWorkbookRow
                {
                    RowNumber = (int?)row.Attribute("r") ?? (dataRows.Count + 2),
                    Cells = cells
                });
            }

            return new ExcelWorkbookSheet
            {
                Headers = headersByColumn
                    .OrderBy(item => item.Key)
                    .Select(item => item.Value)
                    .ToList(),
                Rows = dataRows
            };
        }

        private static XDocument LoadFirstWorksheet(ZipArchive archive)
        {
            var worksheetEntry = archive.Entries
                .Where(entry =>
                    entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                    && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

            if (worksheetEntry == null)
            {
                throw new DomainValidationException("The workbook does not contain any worksheet.", "excel_import_missing_sheet");
            }

            using var stream = worksheetEntry.Open();
            return XDocument.Load(stream);
        }

        private static List<string> LoadSharedStrings(ZipArchive archive)
        {
            var sharedStringsEntry = archive.GetEntry("xl/sharedStrings.xml");
            if (sharedStringsEntry == null)
            {
                return new List<string>();
            }

            var spreadsheetNs = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            using var stream = sharedStringsEntry.Open();
            var document = XDocument.Load(stream);

            return document.Root?
                .Elements(spreadsheetNs + "si")
                .Select(item => string.Concat(item.Descendants(spreadsheetNs + "t").Select(text => (string?)text ?? string.Empty)))
                .ToList()
                ?? new List<string>();
        }

        private static Dictionary<int, string> ReadRowCells(XElement row, IReadOnlyList<string> sharedStrings, XNamespace spreadsheetNs)
        {
            var values = new Dictionary<int, string>();
            var nextColumnIndex = 1;

            foreach (var cell in row.Elements(spreadsheetNs + "c"))
            {
                var cellReference = (string?)cell.Attribute("r");
                var columnIndex = GetColumnIndex(cellReference, nextColumnIndex);
                values[columnIndex] = ReadCellValue(cell, sharedStrings, spreadsheetNs);
                nextColumnIndex = columnIndex + 1;
            }

            return values;
        }

        private static Dictionary<int, string> BuildHeaders(IReadOnlyDictionary<int, string> headerCells)
        {
            var headers = new Dictionary<int, string>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (columnIndex, rawHeader) in headerCells.OrderBy(item => item.Key))
            {
                var baseName = string.IsNullOrWhiteSpace(rawHeader)
                    ? $"Column {columnIndex}"
                    : rawHeader.Trim();

                var uniqueName = baseName;
                var suffix = 2;
                while (!usedNames.Add(uniqueName))
                {
                    uniqueName = $"{baseName} ({suffix})";
                    suffix++;
                }

                headers[columnIndex] = uniqueName;
            }

            return headers;
        }

        private static string ReadCellValue(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace spreadsheetNs)
        {
            var cellType = (string?)cell.Attribute("t");

            if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
            {
                return string.Concat(cell.Descendants(spreadsheetNs + "t").Select(text => (string?)text ?? string.Empty));
            }

            if (cell.Element(spreadsheetNs + "is") is XElement inlineString)
            {
                return string.Concat(inlineString.Descendants(spreadsheetNs + "t").Select(text => (string?)text ?? string.Empty));
            }

            var rawValue = cell.Element(spreadsheetNs + "v")?.Value ?? string.Empty;

            if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(rawValue, out var sharedStringIndex)
                && sharedStringIndex >= 0
                && sharedStringIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedStringIndex];
            }

            return rawValue;
        }

        private static int GetColumnIndex(string? cellReference, int fallbackIndex)
        {
            if (string.IsNullOrWhiteSpace(cellReference))
            {
                return fallbackIndex;
            }

            var letters = new string(cellReference
                .TakeWhile(char.IsLetter)
                .ToArray());

            if (string.IsNullOrWhiteSpace(letters))
            {
                return fallbackIndex;
            }

            var columnIndex = 0;
            foreach (var character in letters.ToUpperInvariant())
            {
                columnIndex = (columnIndex * 26) + (character - 'A' + 1);
            }

            return columnIndex <= 0 ? fallbackIndex : columnIndex;
        }
    }
}
