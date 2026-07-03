using System.Collections.Concurrent;
using Dapper;

namespace SpareParts.Infrastructure.Data
{
    internal static class AccountingSchemaInspector
    {
        // Process-lifetime caches: schema shape (tables/columns) does not change while the
        // process is running, so once we've answered HasTable/HasColumn for a given
        // (connection-kind, table[, column]) we never need to round-trip to the database again.
        // Keyed by connection kind ("sqlite"/"sqlserver") + table name so a process that (in tests)
        // talks to more than one distinct database kind can't get a false cache hit across them.
        private static readonly ConcurrentDictionary<(string ConnectionKind, string Table), bool> TableCache = new();
        private static readonly ConcurrentDictionary<(string ConnectionKind, string Table, string Column), bool> ColumnCache = new();

        public static bool HasTable(DbSession session, string tableName)
        {
            var connectionKind = GetConnectionKind(session);
            var key = (connectionKind, tableName);

            return TableCache.GetOrAdd(key, _ => QueryHasTable(session, tableName, connectionKind == SqliteKind));
        }

        public static bool HasColumn(DbSession session, string tableName, string columnName)
        {
            var connectionKind = GetConnectionKind(session);
            var key = (connectionKind, tableName, columnName);

            return ColumnCache.GetOrAdd(key, _ => QueryHasColumn(session, tableName, columnName, connectionKind == SqliteKind));
        }

        private static bool QueryHasTable(DbSession session, string tableName, bool isSqlite)
        {
            if (isSqlite)
            {
                const string sqliteSql = @"SELECT COUNT(1)
                                           FROM sqlite_master
                                           WHERE type = 'table'
                                             AND name = @TableName;";

                return session.Connection.ExecuteScalar<int>(
                    sqliteSql,
                    new { TableName = NormalizeSqliteName(tableName) },
                    session.Transaction) > 0;
            }

            const string sql = @"SELECT CASE
                                     WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN CAST(0 AS BIT)
                                     ELSE CAST(1 AS BIT)
                                 END;";

            return session.Connection.ExecuteScalar<bool>(sql, new { TableName = tableName }, session.Transaction);
        }

        private static bool QueryHasColumn(DbSession session, string tableName, string columnName, bool isSqlite)
        {
            if (isSqlite)
            {
                const string sqliteSql = @"SELECT COUNT(1)
                                           FROM pragma_table_info(@TableName)
                                           WHERE name = @ColumnName;";

                return session.Connection.ExecuteScalar<int>(sqliteSql, new
                {
                    TableName = NormalizeSqliteName(tableName),
                    ColumnName = columnName
                }, session.Transaction) > 0;
            }

            const string sql = @"SELECT CASE
                                     WHEN COL_LENGTH(@TableName, @ColumnName) IS NULL THEN CAST(0 AS BIT)
                                     ELSE CAST(1 AS BIT)
                                 END;";

            return session.Connection.ExecuteScalar<bool>(sql, new
            {
                TableName = tableName,
                ColumnName = columnName
            }, session.Transaction);
        }

        private const string SqliteKind = "sqlite";
        private const string SqlServerKind = "sqlserver";

        private static string GetConnectionKind(DbSession session)
            => IsSqlite(session) ? SqliteKind : SqlServerKind;

        private static bool IsSqlite(DbSession session)
            => session.Connection.GetType().FullName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        private static string NormalizeSqliteName(string tableName)
        {
            var dotIndex = tableName.LastIndexOf('.');
            return dotIndex >= 0 ? tableName[(dotIndex + 1)..] : tableName;
        }
    }
}
