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
        public SalesApiClient(IApiClient? api = null) : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
            => AddAsync<CreateSaleResponse>("api/sales", req, "Empty sale response.");

        public Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query)
            => RetrieveAsync<SalesInvoiceLookupDto>($"api/sales?search={Uri.EscapeDataString(query ?? string.Empty)}");

        public async Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
        {
            var request = CreateRequest($"api/sales/{invoiceId}", Method.Get);
            var response = await Client.ExecuteAsync<SalesInvoiceDetailsDto?>(request);
            ApiClientBase.EnsureSuccess(response, $"GET api/sales/{invoiceId} failed.");
            return response.Data;
        }
    }
}
