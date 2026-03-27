namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class AccountingRepositories
    {
        internal AccountingRepositories(DbSession session) => Journal = new JournalRepository(session);
        public IJournalRepository Journal { get; }
    }
}
