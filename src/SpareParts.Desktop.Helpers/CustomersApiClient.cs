using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.BusinessPartners;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CustomersApiClient : FeatureApiClientBase, ICustomerApiClient
    {
        public CustomersApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<CustomerDto>> SearchCustomersAsync(string query)
            => RetrieveAsync<CustomerDto>($"api/customers?search={Uri.EscapeDataString(query)}");
    }
}
