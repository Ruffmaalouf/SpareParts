using System.Data;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface ISqlConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
