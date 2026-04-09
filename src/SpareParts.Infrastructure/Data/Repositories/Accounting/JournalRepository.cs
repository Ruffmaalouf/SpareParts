using Dapper;
using SpareParts.Domain.Accounting;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

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

        public void DeleteEntriesByReference(string referenceType, int referenceId)
        {
            const string deleteLinesSql = @"DELETE jl
                FROM JournalLines jl
                INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                WHERE je.ReferenceType = @ReferenceType AND je.ReferenceId = @ReferenceId;";

            const string deleteEntriesSql = @"DELETE FROM JournalEntries
                WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId;";

            var args = new { ReferenceType = referenceType, ReferenceId = referenceId };
            _session.Connection.Execute(deleteLinesSql, args, _session.Transaction);
            _session.Connection.Execute(deleteEntriesSql, args, _session.Transaction);
        }
    }
}
