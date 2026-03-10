using System.Data;
using System.Data.SqlClient;

namespace SpareParts.Infrastructure.Data
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }

    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
    }
}
