using SpareParts.Domain.Purchases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IPurchasesApiClient
    {
        Task<CreatePurchaseResponse> CreatePurchaseAsync(CreatePurchaseRequest request);
        Task<List<PurchaseInvoiceLookupDto>> SearchPurchasesAsync(string query);
        Task<PurchaseInvoiceDetailsDto?> GetPurchaseAsync(int purchaseId);
        Task UpdatePurchaseAsync(int purchaseId, UpdatePurchaseRequest request);
        Task<List<UsedCarPurchaseSummaryDto>> GetUsedCarPurchasesAsync();
        Task<UsedCarPurchaseDetailDto?> GetUsedCarPurchaseAsync(int purchaseId);
        Task<CreateUsedCarPurchaseResponse> CreateUsedCarPurchaseAsync(CreateUsedCarPurchaseRequest request);
        Task<PostUsedCarPurchaseResponse> PostUsedCarPurchaseAsync(int purchaseId);
        Task DeleteUsedCarPurchaseAsync(int purchaseId);
    }
}
