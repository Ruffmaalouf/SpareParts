using System.Data;
using System.Diagnostics;

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
                try
                {
                    Transaction.Rollback();
                }
                catch (Exception ex)
                {
                    // Rollback on an already-closed connection is expected and harmless.
                    // Any other failure is surfaced so it is visible in diagnostic logs.
                    Trace.TraceError($"DbSession rollback failed during dispose: {ex}");
                }
            }

            Transaction.Dispose();
            Connection.Dispose();
        }
    }
}
