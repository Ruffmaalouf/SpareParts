using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class PartExcelWorkbookReader
    {
        private static readonly string[] CodeAliases = { "internalcode", "code", "partcode", "itemcode" };
        private static readonly string[] NameAliases = { "name", "partname", "description" };
        private static readonly string[] BarcodeAliases = { "barcode" };
        private static readonly string[] OemAliases = { "oem", "oemnumber", "oemno", "oemcode" };
        private static readonly string[] CategoryAliases = { "category", "categoryname", "categoryid" };
        private static readonly string[] BrandAliases = { "brand", "brandname", "brandid" };
        private static readonly string[] CostAliases = { "cost", "costprice", "purchaseprice", "buyprice" };
        private static readonly string[] SaleAliases = { "sale", "saleprice", "sellingprice", "retailprice" };
        private static readonly string[] AverageAliases = { "average", "averageprice" };
        private static readonly string[] EstimatedMarketAliases = { "estimatedmarketprice", "marketprice", "expectedprice", "expectedvalue", "estimatedsaleprice" };
        private static readonly string[] CurrencyAliases = { "currency", "currencycode" };
        private static readonly string[] MinStockAliases = { "minstock", "minimumstock", "stockminimum", "minimumqty" };
        private static readonly string[] NotesAliases = { "notes", "note", "remarks" };
        private static readonly string[] ConditionAliases = { "condition", "partcondition" };

        public IReadOnlyList<PartExcelImportRow> Read(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new DomainValidationException("Select an Excel workbook first.", "part_import_file_required");
            }

            if (!File.Exists(filePath))
            {
                throw new DomainValidationException("The selected Excel file was not found.", "part_import_file_missing");
            }

            if (!string.Equals(Path.GetExtension(filePath), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainValidationException("Only .xlsx Excel workbooks are supported.", "part_import_extension_invalid");
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
                throw new DomainValidationException("The selected workbook is empty.", "part_import_empty_workbook");
            }

            var headerCells = ReadRowCells(rows[0], sharedStrings, spreadsheetNs);
            var headerMap = BuildHeaderMap(headerCells);
            EnsureHeaderExists(headerMap, CodeAliases, "Internal Code");
            EnsureHeaderExists(headerMap, NameAliases, "Name");

            var result = new List<PartExcelImportRow>();

            foreach (var row in rows.Skip(1))
            {
                var cellValues = ReadRowCells(row, sharedStrings, spreadsheetNs);
                if (cellValues.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                result.Add(new PartExcelImportRow
                {
                    RowNumber = (int?)row.Attribute("r") ?? (result.Count + 2),
                    InternalCode = GetCellValue(cellValues, headerMap, CodeAliases),
                    Barcode = GetCellValue(cellValues, headerMap, BarcodeAliases),
                    Name = GetCellValue(cellValues, headerMap, NameAliases),
                    OEMNumber = GetCellValue(cellValues, headerMap, OemAliases),
                    Category = GetCellValue(cellValues, headerMap, CategoryAliases),
                    Brand = GetCellValue(cellValues, headerMap, BrandAliases),
                    CostPrice = GetCellValue(cellValues, headerMap, CostAliases),
                    SalePrice = GetCellValue(cellValues, headerMap, SaleAliases),
                    AveragePrice = GetCellValue(cellValues, headerMap, AverageAliases),
                    EstimatedMarketPrice = GetCellValue(cellValues, headerMap, EstimatedMarketAliases),
                    Currency = GetCellValue(cellValues, headerMap, CurrencyAliases),
                    MinStock = GetCellValue(cellValues, headerMap, MinStockAliases),
                    Notes = GetCellValue(cellValues, headerMap, NotesAliases),
                    Condition = GetCellValue(cellValues, headerMap, ConditionAliases)
                });
            }

            return result;
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
                throw new DomainValidationException("The workbook does not contain any worksheet.", "part_import_missing_sheet");
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

        private static Dictionary<string, int> BuildHeaderMap(IReadOnlyDictionary<int, string> headerCells)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var (columnIndex, rawHeader) in headerCells)
            {
                var normalizedHeader = NormalizeKey(rawHeader);
                if (string.IsNullOrWhiteSpace(normalizedHeader) || map.ContainsKey(normalizedHeader))
                {
                    continue;
                }

                map[normalizedHeader] = columnIndex;
            }

            return map;
        }

        private static void EnsureHeaderExists(IReadOnlyDictionary<string, int> headerMap, IEnumerable<string> aliases, string label)
        {
            if (aliases.Any(alias => headerMap.ContainsKey(NormalizeKey(alias))))
            {
                return;
            }

            throw new DomainValidationException($"The workbook must contain a '{label}' column.", "part_import_header_missing");
        }

        private static string? GetCellValue(
            IReadOnlyDictionary<int, string> rowValues,
            IReadOnlyDictionary<string, int> headerMap,
            IEnumerable<string> aliases)
        {
            foreach (var alias in aliases)
            {
                var normalizedAlias = NormalizeKey(alias);
                if (!headerMap.TryGetValue(normalizedAlias, out var columnIndex))
                {
                    continue;
                }

                if (rowValues.TryGetValue(columnIndex, out var value))
                {
                    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                }
            }

            return null;
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
                .TakeWhile(character => char.IsLetter(character))
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

        private static string NormalizeKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
    }
}
