using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class SalesApiClient : ISalesApiClient
    {
        private readonly IApiClient _api;

        public SalesApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req) => _api.CreateSaleAsync(req);

        public Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query) => _api.SearchInvoicesAsync(query);

        public Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId) => _api.GetInvoiceByIdAsync(invoiceId);
    }
}
