using SpareParts.Domain.Purchases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IPurchasesApiClient
    {
        Task<List<UsedCarPurchaseSummaryDto>> GetUsedCarPurchasesAsync();
        Task<CreateUsedCarPurchaseResponse> CreateUsedCarPurchaseAsync(CreateUsedCarPurchaseRequest request);
    }
}
