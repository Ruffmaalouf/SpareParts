using Dapper;
using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Data
{
    public interface IJournalRepository
    {
        int InsertEntry(JournalEntry entry);
        void InsertLines(int entryId, IList<JournalLine> lines);
    }

    public class JournalRepository : IJournalRepository
    {
        private readonly DbSession _session;

        public JournalRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertEntry(JournalEntry entry)
        {
            const string sql = @"INSERT INTO JournalEntries
                (EntryDate, ReferenceType, ReferenceId, Description, CreatedAt, CreatedByUserId)
                VALUES
                (@EntryDate, @ReferenceType, @ReferenceId, @Description, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, entry, _session.Transaction);
        }

        public void InsertLines(int entryId, IList<JournalLine> lines)
        {
            const string sql = @"INSERT INTO JournalLines
                (JournalEntryId, AccountId, Debit, Credit, CreatedAt, CreatedByUserId)
                VALUES
                (@JournalEntryId, @AccountId, @Debit, @Credit, @CreatedAt, @CreatedByUserId);";
            foreach (var line in lines)
            {
                line.JournalEntryId = entryId;
                _session.Connection.Execute(sql, line, _session.Transaction);
            }
        }
    }
}
