using Dapper;

namespace SpareParts.Infrastructure.Data
{
    internal static class AccountingSchemaInspector
    {
        public static bool HasTable(DbSession session, string tableName)
        {
            if (IsSqlite(session))
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

        public static bool HasColumn(DbSession session, string tableName, string columnName)
        {
            if (IsSqlite(session))
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

        private static bool IsSqlite(DbSession session)
            => session.Connection.GetType().FullName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true;

        private static string NormalizeSqliteName(string tableName)
        {
            var dotIndex = tableName.LastIndexOf('.');
            return dotIndex >= 0 ? tableName[(dotIndex + 1)..] : tableName;
        }
    }
}
