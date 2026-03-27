using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PartsApiClient : IPartsApiClient
    {
        private readonly IApiClient _api;

        public PartsApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<List<PartDto>> GetPartsAsync() => _api.GetPartsAsync();
    }
}
