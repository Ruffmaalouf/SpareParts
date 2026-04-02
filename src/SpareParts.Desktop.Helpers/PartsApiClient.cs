using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class PartsApiClient : FeatureApiClientBase, IPartsApiClient
    {
        public PartsApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.InventoryApiBaseUrl)
        {
        }

        public Task<List<PartDto>> GetPartsAsync() => RetrieveAsync<PartDto>("api/parts");
    }
}
