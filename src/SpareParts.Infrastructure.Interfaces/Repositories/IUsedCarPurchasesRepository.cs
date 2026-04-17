using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IUsedCarPurchasesRepository
    {
        int Insert(UsedCarPurchase purchase);
        void InsertLines(int purchaseId, IList<UsedCarPurchaseLine> lines);
        IReadOnlyList<UsedCarPurchaseSummaryDto> GetAll();
    }
}
