using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PartsApiClient : IPartsApiClient
    {
        private readonly IApiClient _api;

        public PartsApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<PartDto>> GetPartsAsync() => _api.GetPartsAsync();
    }
}
