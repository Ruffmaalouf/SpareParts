using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IAccountingStrategy<TDocument>
    {
        List<JournalLine> BuildJournalLines(TDocument doc, int userId);
    }
}
