using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Sales;

namespace SpareParts.Desktop.Wpf
{
    public sealed class SalesApiClient : ISalesApiClient
    {
        private readonly IApiClient _api;

        public SalesApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req) => _api.CreateSaleAsync(req);
    }
}
