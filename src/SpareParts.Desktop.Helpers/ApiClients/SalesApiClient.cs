using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Sales;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class SalesApiClient : FeatureApiClientBase, ISalesApiClient
    {
        public SalesApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.SalesApiBaseUrl)
        {
        }

        public Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
            => AddAsync<CreateSaleResponse>("api/sales", req, "Empty sale response.");

        public Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query)
            => RetrieveAsync<SalesInvoiceLookupDto>($"api/sales?search={Uri.EscapeDataString(query ?? string.Empty)}");

        public Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
            => RetrieveOptionalAsync<SalesInvoiceDetailsDto>($"api/sales/{invoiceId}");

        public async Task UpdateSaleAsync(int invoiceId, UpdateSaleRequest req)
        {
            await EditAsync($"api/sales/{invoiceId}", req);
        }

        public Task<IssueSalePaymentResponse> IssuePaymentAsync(int invoiceId, IssueSalePaymentRequest req)
            => AddAsync<IssueSalePaymentResponse>($"api/sales/{invoiceId}/payments", req, "Empty sale payment response.");
    }
}
