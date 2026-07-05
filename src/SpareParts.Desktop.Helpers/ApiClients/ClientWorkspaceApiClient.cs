using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ClientWorkspaceApiClient : FeatureApiClientBase, IClientWorkspaceApiClient
    {
        public ClientWorkspaceApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<IgnitionClientWorkspaceDto> GetWorkspaceAsync(int customerId)
            => RetrieveOneAsync<IgnitionClientWorkspaceDto>(
                $"api/clients/{customerId}/workspace",
                "Client workspace was empty.");

        public Task<List<IgnitionClientSearchResultDto>> SearchClientsAsync(string query, int take = 10)
            => RetrieveAsync<IgnitionClientSearchResultDto>(
                $"api/clients/search?q={Uri.EscapeDataString(query)}&take={take.ToString(CultureInfo.InvariantCulture)}");

        public Task<List<IgnitionClientInvoiceRowDto>> GetInvoicesAsync(int customerId, int page = 1, int pageSize = 25, string? sort = null, string? status = null)
            => RetrieveAsync<IgnitionClientInvoiceRowDto>(
                BuildPagedResource($"api/clients/{customerId}/invoices", page, pageSize, ("sort", sort), ("status", status)));

        public Task<List<IgnitionClientPaymentRowDto>> GetPaymentsAsync(int customerId, int page = 1, int pageSize = 25, string? sort = null)
            => RetrieveAsync<IgnitionClientPaymentRowDto>(
                BuildPagedResource($"api/clients/{customerId}/payments", page, pageSize, ("sort", sort)));

        public Task<List<QuoteLookupDto>> GetQuotesAsync(int customerId, int page = 1, int pageSize = 25, string? status = null)
            => RetrieveAsync<QuoteLookupDto>(
                BuildPagedResource($"api/clients/{customerId}/quotes", page, pageSize, ("status", status)));

        public Task<List<PartRequestDto>> GetPartRequestsAsync(int customerId, int page = 1, int pageSize = 25, string? status = null)
            => RetrieveAsync<PartRequestDto>(
                BuildPagedResource($"api/clients/{customerId}/part-requests", page, pageSize, ("status", status)));

        public Task<List<IgnitionClientTimelineEventDto>> GetTimelineAsync(int customerId, int page = 1, int pageSize = 25, string? types = null)
            => RetrieveAsync<IgnitionClientTimelineEventDto>(
                BuildPagedResource($"api/clients/{customerId}/timeline", page, pageSize, ("types", types)));

        private static string BuildPagedResource(string basePath, int page, int pageSize, params (string Name, string? Value)[] extras)
        {
            var resource = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?page={1}&pageSize={2}",
                basePath,
                page,
                pageSize);

            foreach (var (name, value) in extras)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    resource = $"{resource}&{name}={Uri.EscapeDataString(value)}";
                }
            }

            return resource;
        }
    }
}
