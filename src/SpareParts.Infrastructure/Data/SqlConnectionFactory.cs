using SpareParts.Infrastructure.Interfaces;
using System.Data;

namespace SpareParts.Infrastructure.Data
{
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var conn = new System.Data.SqlClient.SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
