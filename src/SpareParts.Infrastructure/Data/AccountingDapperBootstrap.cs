using Dapper;

namespace SpareParts.Infrastructure.Data
{
    public static class AccountingDapperBootstrap
    {
        private static bool _initialized;
        private static readonly object SyncRoot = new();

        public static void EnsureConfigured()
        {
            if (_initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (_initialized)
                {
                    return;
                }

                SqlMapper.AddTypeHandler(new AccountTypeTypeHandler());
                _initialized = true;
            }
        }

    }
}
