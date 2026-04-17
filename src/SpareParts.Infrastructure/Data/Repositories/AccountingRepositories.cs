using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class AccountingRepositories
    {
        internal AccountingRepositories(DbSession session)
        {
            Accounts = new AccountsRepository(session);
            Lookups = new AccountingLookupRepository(session);
            PostingSettings = new AccountingPostingSettingsRepository(session);
            Journal = new JournalRepository(session);
        }

        public IAccountsRepository Accounts { get; }
        public AccountingLookupRepository Lookups { get; }
        public AccountingPostingSettingsRepository PostingSettings { get; }
        public IJournalRepository Journal { get; }
    }
}
