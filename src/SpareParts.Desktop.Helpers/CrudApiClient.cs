using SpareParts.Desktop.Wpf.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CrudApiClient : ICrudApiClient
    {
        private readonly IApiClient _api;

        public CrudApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<List<T>> GetAllAsync<T>(string url) => _api.GetAllAsync<T>(url);
        public Task PostAsync(string url, object payload) => _api.PostAsync(url, payload);
        public Task PutAsync(string url, object payload) => _api.PutAsync(url, payload);
        public Task DeleteAsync(string url) => _api.DeleteAsync(url);
    }
}
