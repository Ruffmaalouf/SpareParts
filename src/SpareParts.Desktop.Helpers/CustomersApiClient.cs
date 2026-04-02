using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.BusinessPartners;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CustomersApiClient : FeatureApiClientBase, ICustomerApiClient
    {
        public CustomersApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.SalesApiBaseUrl)
        {
        }

        public Task<List<CustomerDto>> SearchCustomersAsync(string query)
            => RetrieveAsync<CustomerDto>($"api/customers?search={Uri.EscapeDataString(query)}");
    }
}
