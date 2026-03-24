using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Services
{
    public interface IAccountingStrategy<TDocument>
    {
        List<JournalLine> BuildJournalLines(TDocument doc, int userId);
    }
}
