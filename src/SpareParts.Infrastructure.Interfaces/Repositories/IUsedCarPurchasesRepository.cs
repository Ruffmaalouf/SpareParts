using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IUsedCarPurchasesRepository
    {
        int Insert(UsedCarPurchase purchase);
        void InsertLines(int purchaseId, IList<UsedCarPurchaseLine> lines);
        IReadOnlyList<UsedCarPurchaseSummaryDto> GetAll();
        UsedCarPurchaseDetailDto? GetDetail(int id);
        UsedCarPurchaseDetailDto? GetDraftByUsedCarId(int usedCarId);
        bool Update(int id, UsedCarPurchase purchase);
        void ReplaceLines(int purchaseId, IList<UsedCarPurchaseLine> lines);
        bool MarkPosted(int id, DateTime postedAt, int postedByUserId);
        bool HasPostedPurchase(int usedCarId);
        int DeleteDraftsByUsedCarId(int usedCarId);
        bool Delete(int id);
    }
}
