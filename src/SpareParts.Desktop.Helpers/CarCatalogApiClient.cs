using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CarCatalogApiClient : ICarCatalogApiClient
    {
        private readonly IApiClient _api;

        public CarCatalogApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<List<CarBrandDto>> GetCarBrandsAsync() => _api.GetCarBrandsAsync();
        public Task<BitmapImage?> GetCarBrandLogoAsync(int brandId) => _api.GetCarBrandLogoAsync(brandId);
        public Task UploadCarBrandLogoAsync(int brandId, string filePath) => _api.UploadCarBrandLogoAsync(brandId, filePath);
        public Task<List<CarModelDto>> GetCarModelsAsync(int brandId) => _api.GetCarModelsAsync(brandId);
        public Task<BitmapImage?> GetCarModelImageAsync(int modelId) => _api.GetCarModelImageAsync(modelId);
        public Task UploadCarModelImageAsync(int modelId, string filePath) => _api.UploadCarModelImageAsync(modelId, filePath);
    }
}
