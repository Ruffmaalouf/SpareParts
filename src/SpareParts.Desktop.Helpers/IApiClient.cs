using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public interface IApiSessionClient
    {
        void SetToken(string token);
        void ClearToken();
    }

    public interface IAuthApiClient
    {
        Task<LoginResponse> LoginAsync(string username, string password);
        Task<bool> PingAsync();
    }

    public interface IUserApiClient
    {
        Task<List<UserDto>> GetUsersAsync();
        Task<int> CreateUserAsync(CreateUserRequest req);
        Task UpdateUserAsync(int id, UpdateUserRequest req);
        Task DeleteUserAsync(int id);
    }

    public interface IRoleApiClient
    {
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        Task DeleteRoleAsync(int id);
    }

    public interface ICustomerApiClient
    {
        Task<List<CustomerDto>> SearchCustomersAsync(string query);
    }

    public interface IWarehouseApiClient
    {
        Task<List<WarehouseDto>> GetWarehousesAsync();
    }

    public interface ICarCatalogApiClient
    {
        Task<List<CarBrandDto>> GetCarBrandsAsync();
        Task<BitmapImage?> GetCarBrandLogoAsync(int brandId);
        Task UploadCarBrandLogoAsync(int brandId, string filePath);
        Task<List<CarModelDto>> GetCarModelsAsync(int brandId);
        Task<BitmapImage?> GetCarModelImageAsync(int modelId);
        Task UploadCarModelImageAsync(int modelId, string filePath);
    }

    public interface IPartsApiClient
    {
        Task<List<PartDto>> GetPartsAsync();
    }

    public interface ISalesApiClient
    {
        Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);
    }

    public interface ICrudApiClient
    {
        Task<List<T>> GetAllAsync<T>(string url);
        Task PostAsync(string url, object payload);
        Task PutAsync(string url, object payload);
        Task DeleteAsync(string url);
    }

    public interface IApiClient :
        IApiSessionClient,
        IAuthApiClient,
        IUserApiClient,
        IRoleApiClient,
        ICustomerApiClient,
        IWarehouseApiClient,
        ICarCatalogApiClient,
        IPartsApiClient,
        ISalesApiClient,
        ICrudApiClient
    {
    }
}
