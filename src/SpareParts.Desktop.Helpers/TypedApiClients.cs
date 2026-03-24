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
    public sealed class ApiSessionClient : IApiSessionClient
    {
        private readonly IApiClient _api;

        public ApiSessionClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public void SetToken(string token) => _api.SetToken(token);

        public void ClearToken() => _api.ClearToken();
    }

    public sealed class AuthApiClient : IAuthApiClient
    {
        private readonly IApiClient _api;

        public AuthApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<LoginResponse> LoginAsync(string username, string password)
            => _api.LoginAsync(username, password);

        public Task<bool> PingAsync() => _api.PingAsync();
    }

    public sealed class UsersApiClient : IUserApiClient
    {
        private readonly IApiClient _api;

        public UsersApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<UserDto>> GetUsersAsync() => _api.GetUsersAsync();

        public Task<int> CreateUserAsync(CreateUserRequest req) => _api.CreateUserAsync(req);

        public Task UpdateUserAsync(int id, UpdateUserRequest req) => _api.UpdateUserAsync(id, req);

        public Task DeleteUserAsync(int id) => _api.DeleteUserAsync(id);
    }

    public sealed class RolesApiClient : IRoleApiClient
    {
        private readonly IApiClient _api;

        public RolesApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<RoleDto>> GetRolesAsync() => _api.GetRolesAsync();

        public Task<RoleDto> CreateRoleAsync(CreateRoleRequest req) => _api.CreateRoleAsync(req);

        public Task UpdateRoleAsync(int id, UpdateRoleRequest req) => _api.UpdateRoleAsync(id, req);

        public Task DeleteRoleAsync(int id) => _api.DeleteRoleAsync(id);
    }

    public sealed class CarCatalogApiClient : ICarCatalogApiClient
    {
        private readonly IApiClient _api;

        public CarCatalogApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<CarBrandDto>> GetCarBrandsAsync() => _api.GetCarBrandsAsync();

        public Task<BitmapImage?> GetCarBrandLogoAsync(int brandId) => _api.GetCarBrandLogoAsync(brandId);

        public Task UploadCarBrandLogoAsync(int brandId, string filePath) => _api.UploadCarBrandLogoAsync(brandId, filePath);

        public Task<List<CarModelDto>> GetCarModelsAsync(int brandId) => _api.GetCarModelsAsync(brandId);

        public Task<BitmapImage?> GetCarModelImageAsync(int modelId) => _api.GetCarModelImageAsync(modelId);

        public Task UploadCarModelImageAsync(int modelId, string filePath) => _api.UploadCarModelImageAsync(modelId, filePath);
    }

    public sealed class PartsApiClient : IPartsApiClient
    {
        private readonly IApiClient _api;

        public PartsApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<PartDto>> GetPartsAsync() => _api.GetPartsAsync();
    }

    public sealed class SalesApiClient : ISalesApiClient
    {
        private readonly IApiClient _api;

        public SalesApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req) => _api.CreateSaleAsync(req);
    }

    public sealed class CrudApiClient : ICrudApiClient
    {
        private readonly IApiClient _api;

        public CrudApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<T>> GetAllAsync<T>(string url) => _api.GetAllAsync<T>(url);

        public Task PostAsync(string url, object payload) => _api.PostAsync(url, payload);

        public Task PutAsync(string url, object payload) => _api.PutAsync(url, payload);

        public Task DeleteAsync(string url) => _api.DeleteAsync(url);
    }

    public sealed class CustomersApiClient : ICustomerApiClient
    {
        private readonly IApiClient _api;

        public CustomersApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<CustomerDto>> SearchCustomersAsync(string query) => _api.SearchCustomersAsync(query);
    }

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
