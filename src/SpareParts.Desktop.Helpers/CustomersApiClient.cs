using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.BusinessPartners;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CustomersApiClient : ICustomerApiClient
    {
        private readonly IApiClient _api;

        public CustomersApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<CustomerDto>> SearchCustomersAsync(string query) => _api.SearchCustomersAsync(query);
    }
}
