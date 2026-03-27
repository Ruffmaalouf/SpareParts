using SpareParts.Desktop.Wpf.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CrudApiClient : FeatureApiClientBase, ICrudApiClient
    {
        public CrudApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<T>> GetAllAsync<T>(string url) => RetrieveAsync<T>(url);
        public Task PostAsync(string url, object payload) => AddAsync(url, payload);
        public Task PutAsync(string url, object payload) => EditAsync(url, payload);
        public Task DeleteAsync(string url) => DeleteResourceAsync(url);
    }
}
