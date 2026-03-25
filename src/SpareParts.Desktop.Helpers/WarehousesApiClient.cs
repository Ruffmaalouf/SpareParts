using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.MasterData;

namespace SpareParts.Desktop.Wpf
{
    public sealed class WarehousesApiClient : IWarehouseApiClient
    {
        private readonly IApiClient _api;

        public WarehousesApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<WarehouseDto>> GetWarehousesAsync() => _api.GetWarehousesAsync();
    }
}
