using System.Data;

namespace SpareParts.Infrastructure.Data
{
    public sealed class DbSession : IDisposable
    {
        public IDbConnection Connection { get; }
        public IDbTransaction Transaction { get; }

        private bool _committed;

        public DbSession(ISqlConnectionFactory factory)
        {
            Connection = factory.CreateConnection();
            Transaction = Connection.BeginTransaction();
        }

        public void Commit()
        {
            if (_committed) return;
            Transaction.Commit();
            _committed = true;
        }

        public void Rollback()
        {
            if (_committed) return;
            Transaction.Rollback();
        }

        public void Dispose()
        {
            if (!_committed)
            {
                try { Transaction.Rollback(); } catch { }
            }

            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}
