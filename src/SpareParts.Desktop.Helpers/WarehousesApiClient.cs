using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.MasterData;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class WarehousesApiClient : FeatureApiClientBase, IWarehouseApiClient
    {
        public WarehousesApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<WarehouseDto>> GetWarehousesAsync() => RetrieveAsync<WarehouseDto>("api/warehouses");
    }
}
