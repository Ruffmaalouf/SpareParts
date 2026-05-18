using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.ExcelImport;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Data.Repositories.MasterData;
using System.Globalization;

namespace SpareParts.Infrastructure.Services;

public sealed class ExcelImportService
{
    private readonly ISqlConnectionFactory _factory;

    public ExcelImportService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<ExcelImportTableDto> GetTargets()
    {
        using var connection = _factory.CreateConnection();
        var repository = new ExcelImportMetadataRepository(connection);

        var tables = repository.GetTables()
            .Select(table =>
            {
                var columns = repository.GetColumns(table.SchemaName, table.TableName)
                    .Select(PrepareColumn)
                    .ToList();

                table.DisplayName = ToDisplayName(table.TableName);
                table.Description = $"{table.SchemaName}.{table.TableName}";
                table.Columns = columns;
                table.ColumnCount = columns.Count;
                return table;
            })
            .Where(table => table.Columns.Count > 0)
            .ToList();

        return tables;
    }

    public void ImportRow(ExcelImportRowRequest request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.TableKey))
        {
            throw new ValidationException("Choose a system table before importing.");
        }

        using var session = new DbSession(_factory);
        var metadata = new ExcelImportMetadataRepository(session.Connection, session.Transaction);
        var table = metadata.GetTable(request.TableKey)
            ?? throw new NotFoundException($"System table '{request.TableKey}' was not found.");

        var columns = metadata.GetColumns(table.SchemaName, table.TableName)
            .Select(PrepareColumn)
            .ToList();

        if (columns.Count == 0)
        {
            throw new ValidationException($"System table '{table.SchemaName}.{table.TableName}' does not have importable columns.");
        }

        var insertedId = InsertRow(session, metadata, table, columns, request.Values, userId);
        ApplyPostInsertSideEffects(session, metadata, table, request.Values, insertedId, userId);
        session.Commit();
    }

    private static ExcelImportColumnDto PrepareColumn(ExcelImportColumnDto column)
    {
        column.DisplayName = ToDisplayName(column.ColumnName);
        column.DataType = MapSqlType(column.SqlDataType);
        column.Description = BuildDescription(column);
        return column;
    }

    private static int? InsertRow(
        DbSession session,
        ExcelImportMetadataRepository metadata,
        ExcelImportTableDto table,
        IReadOnlyList<ExcelImportColumnDto> columns,
        IReadOnlyDictionary<string, string?> values,
        int userId)
    {
        var insertColumns = new List<string>();
        var parameters = new DynamicParameters();
        var parameterIndex = 0;

        foreach (var column in columns)
        {
            var rawValue = GetMappedValue(values, column);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                if (column.IsRequired)
                {
                    throw new ValidationException($"{column.ColumnName} is required.");
                }

                continue;
            }

            var parameterName = $"p{parameterIndex++}";
            insertColumns.Add(column.ColumnName);
            parameters.Add(parameterName, ConvertValue(metadata, column, rawValue.Trim()));
        }

        AddAuditColumnIfPresent(metadata, table, insertColumns, parameters, ref parameterIndex, "CreatedAt", DateTime.UtcNow);
        AddAuditColumnIfPresent(metadata, table, insertColumns, parameters, ref parameterIndex, "CreatedByUserId", userId);

        if (insertColumns.Count == 0)
        {
            throw new ValidationException("No mapped values were supplied for this row.");
        }

        var quotedColumns = insertColumns
            .Select(ExcelImportMetadataRepository.Quote)
            .ToArray();
        var parameterNames = Enumerable.Range(0, insertColumns.Count)
            .Select(index => $"@p{index}")
            .ToArray();

        var sql = $@"
            INSERT INTO {ExcelImportMetadataRepository.Quote(table.SchemaName)}.{ExcelImportMetadataRepository.Quote(table.TableName)}
            ({string.Join(", ", quotedColumns)})
            VALUES ({string.Join(", ", parameterNames)});
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return session.Connection.ExecuteScalar<int?>(sql, parameters, session.Transaction);
    }

    private static void AddAuditColumnIfPresent(
        ExcelImportMetadataRepository metadata,
        ExcelImportTableDto table,
        ICollection<string> insertColumns,
        DynamicParameters parameters,
        ref int parameterIndex,
        string columnName,
        object value)
    {
        if (insertColumns.Contains(columnName, StringComparer.OrdinalIgnoreCase)
            || !metadata.HasColumn(table.SchemaName, table.TableName, columnName))
        {
            return;
        }

        var parameterName = $"p{parameterIndex++}";
        insertColumns.Add(columnName);
        parameters.Add(parameterName, value);
    }

    private static object ConvertValue(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        var sqlType = column.SqlDataType.Trim().ToLowerInvariant();
        return sqlType switch
        {
            "tinyint" => ParseByte(metadata, column, rawValue),
            "smallint" => ParseShort(metadata, column, rawValue),
            "int" => ParseInt(metadata, column, rawValue),
            "bigint" => ParseLong(metadata, column, rawValue),
            "bit" => ParseBoolean(rawValue),
            "decimal" or "numeric" or "money" or "smallmoney" => ParseDecimal(rawValue, column.ColumnName),
            "float" or "real" => ParseDouble(rawValue, column.ColumnName),
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => ParseDateTime(rawValue, column.ColumnName),
            "uniqueidentifier" => ParseGuid(rawValue, column.ColumnName),
            _ => NormalizeText(rawValue, column)
        };
    }

    private static byte ParseByte(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        if (byte.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        var resolved = ResolveReference(metadata, column, rawValue);
        if (resolved != null && byte.TryParse(Convert.ToString(resolved, CultureInfo.InvariantCulture), out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{column.ColumnName} '{rawValue}' is not a valid whole number.");
    }

    private static short ParseShort(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        if (short.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        var resolved = ResolveReference(metadata, column, rawValue);
        if (resolved != null && short.TryParse(Convert.ToString(resolved, CultureInfo.InvariantCulture), out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{column.ColumnName} '{rawValue}' is not a valid whole number.");
    }

    private static int ParseInt(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        var resolved = ResolveReference(metadata, column, rawValue);
        if (resolved != null && int.TryParse(Convert.ToString(resolved, CultureInfo.InvariantCulture), out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{column.ColumnName} '{rawValue}' is not a valid whole number.");
    }

    private static long ParseLong(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        var resolved = ResolveReference(metadata, column, rawValue);
        if (resolved != null && long.TryParse(Convert.ToString(resolved, CultureInfo.InvariantCulture), out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{column.ColumnName} '{rawValue}' is not a valid whole number.");
    }

    private static object? ResolveReference(ExcelImportMetadataRepository metadata, ExcelImportColumnDto column, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(column.ReferencedTableName))
        {
            return null;
        }

        return metadata.ResolveReferenceValue(column, rawValue);
    }

    private static bool ParseBoolean(string rawValue)
    {
        return NormalizeLookupKey(rawValue) switch
        {
            "1" or "true" or "yes" or "y" or "on" => true,
            "0" or "false" or "no" or "n" or "off" => false,
            _ => throw new ValidationException($"Boolean value '{rawValue}' is invalid. Use True/False, Yes/No, or 1/0.")
        };
    }

    private static decimal ParseDecimal(string rawValue, string columnName)
    {
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid number.");
    }

    private static double ParseDouble(string rawValue, string columnName)
    {
        if (double.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
            || double.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid number.");
    }

    private static DateTime ParseDateTime(string rawValue, string columnName)
    {
        if (DateTime.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var parsed)
            || DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid date.");
    }

    private static Guid ParseGuid(string rawValue, string columnName)
    {
        if (Guid.TryParse(rawValue, out var parsed))
        {
            return parsed;
        }

        throw new ValidationException($"{columnName} '{rawValue}' is not a valid identifier.");
    }

    private static string NormalizeText(string rawValue, ExcelImportColumnDto column)
    {
        var value = rawValue.Trim();
        if (column.MaxLength is > 0 && value.Length > column.MaxLength.Value)
        {
            throw new ValidationException($"{column.ColumnName} is too long. Maximum length is {column.MaxLength.Value} character(s).");
        }

        return value;
    }

    private static string? GetMappedValue(IReadOnlyDictionary<string, string?> values, ExcelImportColumnDto column)
    {
        foreach (var item in values)
        {
            if (string.Equals(item.Key, column.ColumnName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Key, column.Key, StringComparison.OrdinalIgnoreCase))
            {
                return item.Value;
            }
        }

        return null;
    }

    private static void ApplyPostInsertSideEffects(
        DbSession session,
        ExcelImportMetadataRepository metadata,
        ExcelImportTableDto table,
        IReadOnlyDictionary<string, string?> values,
        int? insertedId,
        int userId)
    {
        if (insertedId is not > 0)
        {
            return;
        }

        if (IsTable(table, "Customers"))
        {
            EnsureCustomerAccount(session, metadata, insertedId.Value, GetNamedValue(values, "Name"), userId);
            return;
        }

        if (IsTable(table, "Suppliers"))
        {
            EnsureSupplierAccount(session, metadata, insertedId.Value, GetNamedValue(values, "Name"), userId);
        }
    }

    private static void EnsureCustomerAccount(
        DbSession session,
        ExcelImportMetadataRepository metadata,
        int customerId,
        string? customerName,
        int userId)
    {
        if (!metadata.HasColumn("dbo", "Customers", "AccountId"))
        {
            return;
        }

        var accounting = RepositoryCatalog.For(session).Accounting;
        var parentAccountId = EnsureAccount(accounting, "1200", "Customer Accounts", "asset", null, userId);
        var accountId = EnsureAccount(
            accounting,
            $"CUST-{customerId:D6}",
            BuildAccountName("Customer", customerName, customerId),
            "asset",
            parentAccountId,
            userId);

        new CustomersRepository(session).SetAccountId(customerId, accountId, userId);
    }

    private static void EnsureSupplierAccount(
        DbSession session,
        ExcelImportMetadataRepository metadata,
        int supplierId,
        string? supplierName,
        int userId)
    {
        if (!metadata.HasColumn("dbo", "Suppliers", "AccountId"))
        {
            return;
        }

        var accounting = RepositoryCatalog.For(session).Accounting;
        var accountsPayable = accounting.Accounts.GetByCode("2000");
        var parentAccountId = EnsureAccount(accounting, "2100", "Supplier Accounts", "liability", accountsPayable?.Id, userId);
        var accountId = EnsureAccount(
            accounting,
            $"SUP-{supplierId:000000}",
            BuildAccountName("Supplier", supplierName, supplierId),
            "liability",
            parentAccountId,
            userId);

        new SuppliersRepository(session).SetAccountId(supplierId, accountId, userId);
    }

    private static int EnsureAccount(
        AccountingRepositories accounting,
        string code,
        string name,
        string accountTypeKey,
        int? parentId,
        int userId)
    {
        var existing = accounting.Accounts.GetByCode(code);
        if (existing != null)
        {
            accounting.Accounts.Update(existing.Id, code, name, accountTypeKey, parentId, userId);
            return existing.Id;
        }

        return accounting.Accounts.Insert(new Account
        {
            Code = code,
            Name = name,
            AccountTypeKey = accountTypeKey,
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static string BuildAccountName(string prefix, string? name, int id)
    {
        var safeName = string.IsNullOrWhiteSpace(name)
            ? $"{prefix} {id}"
            : name.Trim();

        return safeName.Length > 149
            ? $"{prefix} - {safeName[..149]}"
            : $"{prefix} - {safeName}";
    }

    private static string? GetNamedValue(IReadOnlyDictionary<string, string?> values, string columnName)
        => values.FirstOrDefault(item => string.Equals(item.Key, columnName, StringComparison.OrdinalIgnoreCase)).Value;

    private static bool IsTable(ExcelImportTableDto table, string tableName)
        => string.Equals(table.SchemaName, "dbo", StringComparison.OrdinalIgnoreCase)
            && string.Equals(table.TableName, tableName, StringComparison.OrdinalIgnoreCase);

    private static string BuildDescription(ExcelImportColumnDto column)
    {
        var required = column.IsRequired ? "Required" : "Optional";
        var relation = string.IsNullOrWhiteSpace(column.ReferencedTableName)
            ? string.Empty
            : $" Linked to {column.ReferencedTableName}.";

        return $"{required} {column.SqlDataType} column.{relation}";
    }

    private static string MapSqlType(string sqlType)
    {
        return sqlType.Trim().ToLowerInvariant() switch
        {
            "tinyint" or "smallint" or "int" or "bigint" => "Integer",
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "Decimal",
            "bit" => "Boolean",
            "date" or "datetime" or "datetime2" or "smalldatetime" or "datetimeoffset" => "Date",
            "uniqueidentifier" => "Identifier",
            _ => "Text"
        };
    }

    private static string ToDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var chars = new List<char>();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0
                && char.IsUpper(current)
                && (char.IsLower(value[index - 1]) || (index + 1 < value.Length && char.IsLower(value[index + 1]))))
            {
                chars.Add(' ');
            }

            chars.Add(current == '_' ? ' ' : current);
        }

        return new string(chars.ToArray()).Trim();
    }

    private static string NormalizeLookupKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }
}
