using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using System.Windows.Media.Imaging;

namespace SpareParts.ManagementTests;

internal sealed class StubCarCatalogApiClient : ICarCatalogApiClient
{
    public Task<List<CarBrandDto>> GetCarBrandsAsync() => Task.FromResult(new List<CarBrandDto>());
    public Task<BitmapImage?> GetCarBrandLogoAsync(int brandId) => Task.FromResult<BitmapImage?>(null);
    public Task UploadCarBrandLogoAsync(int brandId, string filePath) => Task.CompletedTask;
    public Task<List<CarModelDto>> GetCarModelsAsync(int brandId) => Task.FromResult(new List<CarModelDto>());
    public Task<BitmapImage?> GetCarModelImageAsync(int modelId) => Task.FromResult<BitmapImage?>(null);
    public Task UploadCarModelImageAsync(int modelId, string filePath) => Task.CompletedTask;
    public Task<List<UsedCarImageDto>> GetUsedCarImagesAsync(int usedCarId) => Task.FromResult(new List<UsedCarImageDto>());
    public Task UploadUsedCarImageAsync(int usedCarId, string filePath) => Task.CompletedTask;
    public Task DeleteUsedCarImageAsync(int imageId) => Task.CompletedTask;
}
