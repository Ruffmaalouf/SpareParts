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

        public Task<CreatePurchaseResponse> CreatePurchaseAsync(CreatePurchaseRequest request)
            => AddAsync<CreatePurchaseResponse>("api/purchases", request, "Purchase creation did not return a response.");

        public Task<List<PurchaseInvoiceLookupDto>> SearchPurchasesAsync(string query)
            => RetrieveAsync<PurchaseInvoiceLookupDto>($"api/purchases?search={Uri.EscapeDataString(query ?? string.Empty)}");

        public Task<PurchaseInvoiceDetailsDto?> GetPurchaseAsync(int purchaseId)
            => RetrieveOptionalAsync<PurchaseInvoiceDetailsDto>($"api/purchases/{purchaseId}");

        public async Task UpdatePurchaseAsync(int purchaseId, UpdatePurchaseRequest request)
        {
            await EditAsync($"api/purchases/{purchaseId}", request);
        }

        public Task<List<UsedCarPurchaseSummaryDto>> GetUsedCarPurchasesAsync()
            => RetrieveAsync<UsedCarPurchaseSummaryDto>("api/purchases/used-cars");

        public Task<UsedCarPurchaseDetailDto?> GetUsedCarPurchaseAsync(int purchaseId)
            => RetrieveOptionalAsync<UsedCarPurchaseDetailDto>($"api/purchases/used-cars/{purchaseId}");

        public Task<CreateUsedCarPurchaseResponse> CreateUsedCarPurchaseAsync(CreateUsedCarPurchaseRequest request)
            => AddAsync<CreateUsedCarPurchaseResponse>("api/purchases/used-cars", request, "Used-car purchase creation did not return a response.");

        public Task<PostUsedCarPurchaseResponse> PostUsedCarPurchaseAsync(int purchaseId)
            => AddAsync<PostUsedCarPurchaseResponse>($"api/purchases/used-cars/{purchaseId}/post", new { }, "Used-car purchase posting did not return a response.");

        public Task DeleteUsedCarPurchaseAsync(int purchaseId)
            => DeleteResourceAsync($"api/purchases/used-cars/{purchaseId}");
    }
}
