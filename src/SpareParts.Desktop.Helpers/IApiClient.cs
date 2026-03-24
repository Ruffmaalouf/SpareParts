using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public interface IApiClient
    {
        void SetToken(string token);
        void ClearToken();

        Task<LoginResponse> LoginAsync(string username, string password);
        Task<bool> PingAsync();

        Task<List<UserDto>> GetUsersAsync();
        Task<int> CreateUserAsync(CreateUserRequest req);
        Task UpdateUserAsync(int id, UpdateUserRequest req);
        Task DeleteUserAsync(int id);

        Task<List<CustomerDto>> SearchCustomersAsync(string query);
        Task<List<WarehouseDto>> GetWarehousesAsync();

        Task<List<CarBrandDto>> GetCarBrandsAsync();
        Task<BitmapImage?> GetCarBrandLogoAsync(int brandId);
        Task UploadCarBrandLogoAsync(int brandId, string filePath);

        Task<List<CarModelDto>> GetCarModelsAsync(int brandId);
        Task<BitmapImage?> GetCarModelImageAsync(int modelId);
        Task UploadCarModelImageAsync(int modelId, string filePath);

        Task<List<PartDto>> GetPartsAsync();
        Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);

        Task<List<T>> GetAllAsync<T>(string url);
        Task PostAsync(string url, object payload);
        Task PutAsync(string url, object payload);
        Task DeleteAsync(string url);

        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        Task DeleteRoleAsync(int id);
    }
}
