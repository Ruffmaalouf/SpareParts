using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface ICarCatalogApiClient
    {
        Task<List<CarBrandDto>> GetCarBrandsAsync();
        Task<BitmapImage?> GetCarBrandLogoAsync(int brandId);
        Task UploadCarBrandLogoAsync(int brandId, string filePath);
        Task<List<CarModelDto>> GetCarModelsAsync(int brandId);
        Task<BitmapImage?> GetCarModelImageAsync(int modelId);
        Task UploadCarModelImageAsync(int modelId, string filePath);
        Task<List<UsedCarImageDto>> GetUsedCarImagesAsync(int usedCarId);
        Task UploadUsedCarImageAsync(int usedCarId, string filePath);
        Task DeleteUsedCarImageAsync(int imageId);
    }
}
