using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Purchases;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PurchasesApiClient : FeatureApiClientBase, IPurchasesApiClient
    {
        public PurchasesApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.PurchasesApiBaseUrl)
        {
        }

        public Task<List<UsedCarPurchaseSummaryDto>> GetUsedCarPurchasesAsync()
            => RetrieveAsync<UsedCarPurchaseSummaryDto>("api/purchases/used-cars");

        public Task<CreateUsedCarPurchaseResponse> CreateUsedCarPurchaseAsync(CreateUsedCarPurchaseRequest request)
            => AddAsync<CreateUsedCarPurchaseResponse>("api/purchases/used-cars", request, "Used-car purchase creation did not return a response.");
    }
}
