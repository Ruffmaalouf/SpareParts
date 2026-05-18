using Dapper;
using SpareParts.Domain.ExcelImport;
using System.Data;

namespace SpareParts.Infrastructure.Data.Repositories.MasterData;

public sealed class ExcelImportMetadataRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;

    public ExcelImportMetadataRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public IReadOnlyList<ExcelImportTableDto> GetTables()
    {
        const string sql = @"
            SELECT CONCAT(s.name, '.', t.name) AS [Key],
                   s.name AS SchemaName,
                   t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
              AND s.name NOT IN ('sys', 'INFORMATION_SCHEMA')
              AND EXISTS
              (
                  SELECT 1
                  FROM sys.columns c
                  INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
                  WHERE c.object_id = t.object_id
                    AND c.is_identity = 0
                    AND c.is_computed = 0
                    AND ty.name NOT IN ('timestamp', 'rowversion', 'image', 'binary', 'varbinary', 'xml', 'geography', 'geometry', 'hierarchyid')
                    AND c.name NOT IN ('CreatedAt', 'CreatedByUserId', 'ModifiedAt', 'ModifiedByUserId')
              )
            ORDER BY s.name, t.name;";

        return _connection.Query<ExcelImportTableDto>(sql, transaction: _transaction).ToList();
    }

    public ExcelImportTableDto? GetTable(string tableKey)
    {
        var (schemaName, tableName) = ParseTableKey(tableKey);

        const string sql = @"
            SELECT CONCAT(s.name, '.', t.name) AS [Key],
                   s.name AS SchemaName,
                   t.name AS TableName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            WHERE t.is_ms_shipped = 0
              AND s.name = @SchemaName
              AND t.name = @TableName;";

        return _connection.QueryFirstOrDefault<ExcelImportTableDto>(
            sql,
            new { SchemaName = schemaName, TableName = tableName },
            _transaction);
    }

    public IReadOnlyList<ExcelImportColumnDto> GetColumns(string schemaName, string tableName)
    {
        const string sql = @"
            SELECT CONCAT(s.name, '.', t.name, '.', c.name) AS [Key],
                   c.name AS ColumnName,
                   ty.name AS SqlDataType,
                   CASE
                       WHEN c.is_nullable = 0 AND c.default_object_id = 0 THEN CAST(1 AS bit)
                       ELSE CAST(0 AS bit)
                   END AS IsRequired,
                   CAST(c.is_nullable AS bit) AS IsNullable,
                   CASE WHEN c.default_object_id = 0 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasDefaultValue,
                   CASE
                       WHEN ty.name IN ('nvarchar', 'nchar') AND c.max_length > 0 THEN c.max_length / 2
                       WHEN c.max_length = -1 THEN -1
                       ELSE c.max_length
                   END AS MaxLength,
                   CAST(c.precision AS int) AS Precision,
                   CAST(c.scale AS int) AS Scale,
                   c.column_id AS OrdinalPosition,
                   rs.name AS ReferencedSchemaName,
                   rt.name AS ReferencedTableName,
                   rc.name AS ReferencedColumnName
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.columns c ON c.object_id = t.object_id
            INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN sys.foreign_key_columns fkc ON fkc.parent_object_id = c.object_id
                                                  AND fkc.parent_column_id = c.column_id
            LEFT JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id
                                    AND rc.column_id = fkc.referenced_column_id
            LEFT JOIN sys.tables rt ON rt.object_id = fkc.referenced_object_id
            LEFT JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
            WHERE s.name = @SchemaName
              AND t.name = @TableName
              AND c.is_identity = 0
              AND c.is_computed = 0
              AND ty.name NOT IN ('timestamp', 'rowversion', 'image', 'binary', 'varbinary', 'xml', 'geography', 'geometry', 'hierarchyid')
              AND c.name NOT IN ('CreatedAt', 'CreatedByUserId', 'ModifiedAt', 'ModifiedByUserId')
            ORDER BY c.column_id;";

        return _connection.Query<ExcelImportColumnDto>(
                sql,
                new { SchemaName = schemaName, TableName = tableName },
                _transaction)
            .ToList();
    }

    public bool HasColumn(string schemaName, string tableName, string columnName)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.columns c ON c.object_id = t.object_id
            WHERE s.name = @SchemaName
              AND t.name = @TableName
              AND c.name = @ColumnName;";

        return _connection.ExecuteScalar<int>(
            sql,
            new { SchemaName = schemaName, TableName = tableName, ColumnName = columnName },
            _transaction) > 0;
    }

    public object? ResolveReferenceValue(ExcelImportColumnDto column, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(column.ReferencedSchemaName)
            || string.IsNullOrWhiteSpace(column.ReferencedTableName)
            || string.IsNullOrWhiteSpace(column.ReferencedColumnName))
        {
            return null;
        }

        var lookupColumns = GetReferenceLookupColumns(column.ReferencedSchemaName, column.ReferencedTableName);
        if (lookupColumns.Count == 0)
        {
            return null;
        }

        var predicates = lookupColumns
            .Select(name => $"{Quote(name)} = @LookupValue")
            .ToArray();

        var sql = $@"
            SELECT TOP 1 {Quote(column.ReferencedColumnName)}
            FROM {Quote(column.ReferencedSchemaName)}.{Quote(column.ReferencedTableName)}
            WHERE {string.Join(" OR ", predicates)}
            ORDER BY {Quote(column.ReferencedColumnName)};";

        return _connection.ExecuteScalar(sql, new { LookupValue = rawValue.Trim() }, _transaction);
    }

    public static (string SchemaName, string TableName) ParseTableKey(string tableKey)
    {
        if (string.IsNullOrWhiteSpace(tableKey))
        {
            return ("dbo", string.Empty);
        }

        var parts = tableKey.Trim().Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => ("dbo", parts[0]),
            2 => (parts[0], parts[1]),
            _ => ("dbo", string.Empty)
        };
    }

    public static string Quote(string identifier)
        => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private IReadOnlyList<string> GetReferenceLookupColumns(string schemaName, string tableName)
    {
        const string sql = @"
            SELECT c.name
            FROM sys.tables t
            INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
            INNER JOIN sys.columns c ON c.object_id = t.object_id
            INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.name = @SchemaName
              AND t.name = @TableName
              AND ty.name IN ('nvarchar', 'varchar', 'nchar', 'char')
              AND c.name IN ('Name', 'Code', 'Username', 'FullName', 'Label', 'TypeKey', 'RoleKey', 'InternalCode', 'Email', 'Number', 'PurchaseNumber', 'InvoiceNumber')
            ORDER BY CASE c.name
                WHEN 'Code' THEN 1
                WHEN 'Name' THEN 2
                WHEN 'InternalCode' THEN 3
                WHEN 'Username' THEN 4
                WHEN 'FullName' THEN 5
                ELSE 20
            END, c.column_id;";

        return _connection.Query<string>(
                sql,
                new { SchemaName = schemaName, TableName = tableName },
                _transaction)
            .ToList();
    }
}
