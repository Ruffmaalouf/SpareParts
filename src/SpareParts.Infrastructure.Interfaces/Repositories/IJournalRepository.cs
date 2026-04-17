using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IJournalRepository
    {
        int InsertEntry(JournalEntry entry);
        void InsertLines(int journalEntryId, IList<JournalLine> lines);
        void DeleteEntriesByReference(string referenceType, int referenceId);
        IReadOnlyList<JournalEntrySummaryDto> GetEntries(DateTime? dateFrom, DateTime? dateTo);
        JournalEntryDetailDto? GetEntryDetail(int id);
        IReadOnlyList<LedgerRowDto> GetLedgerRows(int accountId, DateTime? dateFrom, DateTime? dateTo);
        decimal GetOpeningBalance(int accountId, DateTime? dateFrom);
        IReadOnlyList<TrialBalanceRowDto> GetTrialBalanceRows(DateTime? dateFrom, DateTime? dateTo);
        bool HasEntriesForAccount(int accountId);
    }
}
