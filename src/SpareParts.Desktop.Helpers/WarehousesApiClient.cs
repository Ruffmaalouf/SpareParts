using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class WarehousesApiClient : IWarehouseApiClient
    {
        private readonly IApiClient _api;

        public WarehousesApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<List<WarehouseDto>> GetWarehousesAsync() => _api.GetWarehousesAsync();
    }
}
