using Dapper;

namespace SpareParts.Infrastructure.Data
{
    internal static class AccountingSchemaInspector
    {
        public static bool HasTable(DbSession session, string tableName)
        {
            const string sql = @"SELECT CASE
                                     WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN CAST(0 AS BIT)
                                     ELSE CAST(1 AS BIT)
                                 END;";

            return session.Connection.ExecuteScalar<bool>(sql, new { TableName = tableName }, session.Transaction);
        }

        public static bool HasColumn(DbSession session, string tableName, string columnName)
        {
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
    }
}
