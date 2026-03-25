using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IJournalRepository
    {
        int InsertEntry(JournalEntry entry);
        void InsertLines(int journalEntryId, IList<JournalLine> lines);
    }
}
