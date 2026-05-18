using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.ExcelImport;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportCoordinator
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

        private readonly ICrudApiClient _crudApi;
        private IReadOnlyDictionary<string, ExcelImportTargetDefinition> _targets =
            new Dictionary<string, ExcelImportTargetDefinition>(StringComparer.OrdinalIgnoreCase);

        public ExcelImportCoordinator(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
        }

        public async Task LoadTargetsAsync()
        {
            var tables = await _crudApi.GetAllAsync<ExcelImportTableDto>("api/excelimport/targets");
            _targets = tables
                .Select(ToTargetDefinition)
                .ToDictionary(target => target.Key, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ExcelImportTargetOption> GetTargets()
            => _targets.Values
                .OrderBy(target => target.Name, StringComparer.OrdinalIgnoreCase)
                .Select(target => new ExcelImportTargetOption
                {
                    Key = target.Key,
                    Name = target.Name,
                    TableName = target.TableName,
                    Description = target.Description,
                    ColumnCount = target.Fields.Count
                })
                .ToList();

        public IReadOnlyList<ExcelImportFieldDefinition> GetFields(string targetKey)
            => GetTarget(targetKey).Fields;

        public string? GetTargetKeyForTable(string tableName)
            => _targets.Values
                .FirstOrDefault(target =>
                    string.Equals(target.TableName, tableName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(target.Name, tableName, StringComparison.OrdinalIgnoreCase))?
                .Key;

        public ExcelWorkbookSheet ReadWorkbook(string filePath)
            => new ExcelWorkbookReader().Read(filePath);

        public ExcelImportProfile GetProfile(IEnumerable<AppConstantDto>? appConstants, string targetKey)
        {
            if (appConstants == null)
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }

            var profileValue = appConstants
                .FirstOrDefault(item => string.Equals(item.Key, BuildProfileKey(targetKey), StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (string.IsNullOrWhiteSpace(profileValue))
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }

            try
            {
                var profile = JsonSerializer.Deserialize<ExcelImportProfile>(profileValue, JsonOptions);
                if (profile == null)
                {
                    return new ExcelImportProfile { TargetKey = targetKey };
                }

                profile.TargetKey = string.IsNullOrWhiteSpace(profile.TargetKey) ? targetKey : profile.TargetKey;
                profile.Mappings ??= new Dictionary<string, string>();
                return profile;
            }
            catch
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }
        }

        public IReadOnlyDictionary<string, string?> ResolveMappings(
            string targetKey,
            IReadOnlyList<string> workbookHeaders,
            IEnumerable<AppConstantDto>? appConstants,
            IReadOnlyDictionary<string, string?>? overrides = null)
        {
            var target = GetTarget(targetKey);
            var savedProfile = GetProfile(appConstants, targetKey);
            var headers = workbookHeaders
                .Where(header => !string.IsNullOrWhiteSpace(header))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in target.Fields)
            {
                var selectedHeader = ResolveHeaderFromMappings(overrides, field.Key, headers)
                    ?? ResolveHeaderFromProfile(savedProfile, field.Key, headers)
                    ?? FindMatchingHeader(field, headers);

                resolved[field.Key] = selectedHeader;
            }

            return resolved;
        }

        public async Task SaveProfileAsync(string targetKey, IReadOnlyDictionary<string, string?> mappings)
        {
            var target = GetTarget(targetKey);
            var profile = new ExcelImportProfile
            {
                TargetKey = target.Key,
                Mappings = mappings
                    .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                    .ToDictionary(item => item.Key, item => item.Value!.Trim(), StringComparer.OrdinalIgnoreCase)
            };

            await _crudApi.PutAsync(
                $"api/appconstants/{Uri.EscapeDataString(BuildProfileKey(targetKey))}",
                new UpsertAppConstantRequest
                {
                    Value = JsonSerializer.Serialize(profile, JsonOptions),
                    Description = $"Excel import profile for {target.TableName}."
                });
        }

        public async Task<ExcelImportResult> ImportAsync(
            string targetKey,
            string filePath,
            ExcelImportExecutionContext context,
            IReadOnlyDictionary<string, string?>? mappings = null,
            IEnumerable<AppConstantDto>? appConstants = null)
        {
            _ = context;

            var target = GetTarget(targetKey);
            var workbook = ReadWorkbook(filePath);
            var resolvedMappings = ResolveMappings(target.Key, workbook.Headers, appConstants, mappings);
            var result = new ExcelImportResult { TargetName = target.TableName };

            if (workbook.RowCount == 0)
            {
                result.AddError("The workbook does not contain any data rows to import.");
                return result;
            }

            var missingRequiredMappings = target.Fields
                .Where(field => field.IsRequired
                    && (!resolvedMappings.TryGetValue(field.Key, out var selectedHeader)
                        || string.IsNullOrWhiteSpace(selectedHeader)))
                .Select(field => field.ColumnName)
                .ToList();

            if (missingRequiredMappings.Count > 0)
            {
                result.AddError($"Map the required columns first: {string.Join(", ", missingRequiredMappings)}.");
                return result;
            }

            foreach (var row in workbook.Rows)
            {
                var values = BuildRowValues(target, row, resolvedMappings);
                if (values.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                try
                {
                    await _crudApi.PostAsync(
                        "api/excelimport/rows",
                        new ExcelImportRowRequest
                        {
                            TableKey = target.Key,
                            Values = values.ToDictionary(
                                item => item.Key,
                                item => item.Value,
                                StringComparer.OrdinalIgnoreCase)
                        });

                    result.AddImported();
                }
                catch (Exception ex)
                {
                    result.AddError($"Row {row.RowNumber}: {MapImportError(ex)}");
                }
            }

            if (!result.HasImportedRows && !result.HasErrors)
            {
                result.AddError("No mapped rows were found to import.");
            }

            return result;
        }

        private static ExcelImportTargetDefinition ToTargetDefinition(ExcelImportTableDto table)
            => new()
            {
                Key = table.Key,
                Name = string.IsNullOrWhiteSpace(table.DisplayName) ? table.TableName : table.DisplayName,
                TableName = $"{table.SchemaName}.{table.TableName}",
                Description = table.Description,
                Fields = table.Columns
                    .OrderBy(column => column.OrdinalPosition)
                    .Select(ToFieldDefinition)
                    .ToList()
            };

        private static ExcelImportFieldDefinition ToFieldDefinition(ExcelImportColumnDto column)
            => new()
            {
                Key = column.ColumnName,
                ColumnName = column.ColumnName,
                Description = column.Description,
                DataType = MapDataType(column.DataType),
                IsRequired = column.IsRequired,
                Aliases = BuildAliases(column)
            };

        private static IReadOnlyList<string> BuildAliases(ExcelImportColumnDto column)
        {
            var aliases = new List<string>
            {
                column.ColumnName,
                column.DisplayName,
                column.ColumnName.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            };

            AddSuffixAlias(aliases, column.ColumnName, "Id");
            AddSuffixAlias(aliases, column.ColumnName, "Code");

            return aliases
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void AddSuffixAlias(ICollection<string> aliases, string columnName, string suffix)
        {
            if (columnName.Length <= suffix.Length
                || !columnName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            aliases.Add(columnName[..^suffix.Length]);
        }

        private static ExcelImportDataType MapDataType(string dataType)
            => dataType.Trim().ToLowerInvariant() switch
            {
                "integer" => ExcelImportDataType.Integer,
                "decimal" => ExcelImportDataType.Decimal,
                "boolean" => ExcelImportDataType.Boolean,
                "date" => ExcelImportDataType.Date,
                "identifier" => ExcelImportDataType.Identifier,
                _ => ExcelImportDataType.Text
            };

        private static IReadOnlyDictionary<string, string?> BuildRowValues(
            ExcelImportTargetDefinition target,
            ExcelWorkbookRow row,
            IReadOnlyDictionary<string, string?> resolvedMappings)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in target.Fields)
            {
                string? value = null;
                if (resolvedMappings.TryGetValue(field.Key, out var selectedHeader)
                    && !string.IsNullOrWhiteSpace(selectedHeader)
                    && row.Cells.TryGetValue(selectedHeader, out var rawValue))
                {
                    value = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
                }

                values[field.Key] = value;
            }

            return values;
        }

        private static string? ResolveHeaderFromMappings(
            IReadOnlyDictionary<string, string?>? mappings,
            string fieldKey,
            IReadOnlyList<string> workbookHeaders)
        {
            if (mappings == null
                || !mappings.TryGetValue(fieldKey, out var selectedHeader)
                || string.IsNullOrWhiteSpace(selectedHeader))
            {
                return null;
            }

            return workbookHeaders.FirstOrDefault(header =>
                string.Equals(header, selectedHeader, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ResolveHeaderFromProfile(
            ExcelImportProfile profile,
            string fieldKey,
            IReadOnlyList<string> workbookHeaders)
        {
            if (profile.Mappings == null
                || !profile.Mappings.TryGetValue(fieldKey, out var selectedHeader)
                || string.IsNullOrWhiteSpace(selectedHeader))
            {
                return null;
            }

            return workbookHeaders.FirstOrDefault(header =>
                string.Equals(header, selectedHeader, StringComparison.OrdinalIgnoreCase));
        }

        private static string? FindMatchingHeader(ExcelImportFieldDefinition field, IReadOnlyList<string> workbookHeaders)
        {
            var aliases = new List<string> { field.Key, field.ColumnName };
            aliases.AddRange(field.Aliases);

            var normalizedAliases = aliases
                .Select(NormalizeLookupKey)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return workbookHeaders.FirstOrDefault(header =>
                normalizedAliases.Contains(NormalizeLookupKey(header), StringComparer.OrdinalIgnoreCase));
        }

        private ExcelImportTargetDefinition GetTarget(string targetKey)
        {
            if (_targets.TryGetValue(targetKey, out var target))
            {
                return target;
            }

            throw new DomainValidationException($"Unknown Excel import target '{targetKey}'.", "excel_import_target_missing");
        }

        private static string NormalizeLookupKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static string BuildProfileKey(string targetKey)
            => $"ExcelImportProfile.{targetKey.Trim().ToLowerInvariant()}";

        private static string MapImportError(Exception exception)
            => exception switch
            {
                DomainValidationException validation => validation.Message,
                ApiClientException apiException => $"API error ({apiException.Code}): {apiException.Message}",
                _ when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
                _ => "Unexpected error while importing Excel rows."
            };
    }
}
