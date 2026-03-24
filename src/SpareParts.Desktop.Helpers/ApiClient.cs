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

        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        Task DeleteRoleAsync(int id);
    }

    public abstract class ApiClientBase : IApiClient
    {
        protected readonly HttpClient Http;

        protected ApiClientBase(string baseUrl)
        {
            Http = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            Http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public virtual void SetToken(string token)
            => Http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

        public virtual void ClearToken()
            => Http.DefaultRequestHeaders.Authorization = null;

        public abstract Task<LoginResponse> LoginAsync(string username, string password);
        public abstract Task<bool> PingAsync();
        public abstract Task<List<UserDto>> GetUsersAsync();
        public abstract Task<int> CreateUserAsync(CreateUserRequest req);
        public abstract Task UpdateUserAsync(int id, UpdateUserRequest req);
        public abstract Task DeleteUserAsync(int id);
        public abstract Task<List<CustomerDto>> SearchCustomersAsync(string query);
        public abstract Task<List<WarehouseDto>> GetWarehousesAsync();
        public abstract Task<List<CarBrandDto>> GetCarBrandsAsync();
        public abstract Task<BitmapImage?> GetCarBrandLogoAsync(int brandId);
        public abstract Task UploadCarBrandLogoAsync(int brandId, string filePath);
        public abstract Task<List<CarModelDto>> GetCarModelsAsync(int brandId);
        public abstract Task<BitmapImage?> GetCarModelImageAsync(int modelId);
        public abstract Task UploadCarModelImageAsync(int modelId, string filePath);
        public abstract Task<List<PartDto>> GetPartsAsync();
        public abstract Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req);
        public abstract Task<List<T>> GetAllAsync<T>(string url);
        public abstract Task PostAsync(string url, object payload);
        public abstract Task PutAsync(string url, object payload);
        public abstract Task<List<RoleDto>> GetRolesAsync();
        public abstract Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        public abstract Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        public abstract Task DeleteRoleAsync(int id);

        protected static BitmapImage BytesToBitmap(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        protected static string GetMimeType(string path) =>
            Path.GetExtension(path).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "image/png"
            };
    }

    public class ApiClient : ApiClientBase
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static IApiClient Instance { get; } = new ApiClient();

        private ApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        // ── Auth ──────────────────────────────────────────────────────────────
        public override async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var resp = await Http.PostAsJsonAsync("api/auth/login",
                new LoginRequest { Username = username, Password = password });

            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                throw new UnauthorizedAccessException(
                    msg.Trim('"', ' ', '\n') is { Length: > 0 } m ? m : "Invalid credentials.");
            }

            return await resp.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Empty login response.");
        }

        public override async Task<bool> PingAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
                await Http.GetAsync("api/health", cts.Token);
                return true;
            }
            catch { return false; }
        }

        // ── Users ─────────────────────────────────────────────────────────────
        public override async Task<List<UserDto>> GetUsersAsync()
        {
            return await Http.GetFromJsonAsync<List<UserDto>>("api/users")
                   ?? new List<UserDto>();
        }

        public override async Task<int> CreateUserAsync(CreateUserRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/users", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
            return await resp.Content.ReadFromJsonAsync<int>();
        }

        public override async Task UpdateUserAsync(int id, UpdateUserRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/users/{id}", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
        }

        public override async Task DeleteUserAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Deactivate failed: {resp.StatusCode}");
        }

        // ── Customers ─────────────────────────────────────────────────────────
        public override async Task<List<CustomerDto>> SearchCustomersAsync(string query)
        {
            return await Http.GetFromJsonAsync<List<CustomerDto>>(
                       $"api/customers?search={Uri.EscapeDataString(query)}")
                   ?? new List<CustomerDto>();
        }

        // ── Warehouses ────────────────────────────────────────────────────────
        public override async Task<List<WarehouseDto>> GetWarehousesAsync()
        {
            return await Http.GetFromJsonAsync<List<WarehouseDto>>("api/warehouses")
                   ?? new List<WarehouseDto>();
        }

        // ── Car Brands ────────────────────────────────────────────────────────
        public override async Task<List<CarBrandDto>> GetCarBrandsAsync()
        {
            return await Http.GetFromJsonAsync<List<CarBrandDto>>("api/carbrands")
                   ?? new List<CarBrandDto>();
        }

        public override async Task<BitmapImage?> GetCarBrandLogoAsync(int brandId)
        {
            try
            {
                var resp = await Http.GetAsync($"api/carbrands/{brandId}/logo");
                if (!resp.IsSuccessStatusCode) return null;
                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch { return null; }
        }

        public override async Task UploadCarBrandLogoAsync(int brandId, string filePath)
        {
            await using var fs = File.OpenRead(filePath);
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await Http.PostAsync($"api/carbrands/{brandId}/logo", content)).EnsureSuccessStatusCode();
        }

        // ── Car Models ────────────────────────────────────────────────────────
        public override async Task<List<CarModelDto>> GetCarModelsAsync(int brandId)
        {
            return await Http.GetFromJsonAsync<List<CarModelDto>>(
                       $"api/carmodels?brandId={brandId}")
                   ?? new List<CarModelDto>();
        }

        public override async Task<BitmapImage?> GetCarModelImageAsync(int modelId)
        {
            try
            {
                var resp = await Http.GetAsync($"api/carmodels/{modelId}/image");
                if (!resp.IsSuccessStatusCode) return null;
                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch { return null; }
        }

        public override async Task UploadCarModelImageAsync(int modelId, string filePath)
        {
            await using var fs = File.OpenRead(filePath);
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await Http.PostAsync($"api/carmodels/{modelId}/image", content)).EnsureSuccessStatusCode();
        }

        // ── Parts ─────────────────────────────────────────────────────────────
        public override async Task<List<PartDto>> GetPartsAsync()
        {
            return await Http.GetFromJsonAsync<List<PartDto>>("api/parts")
                   ?? new List<PartDto>();
        }

        // ── Sales ─────────────────────────────────────────────────────────────
        public override async Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/sales", req);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                throw new Exception(msg.Trim('"', ' ') is { Length: > 0 } m ? m : resp.StatusCode.ToString());
            }
            return await resp.Content.ReadFromJsonAsync<CreateSaleResponse>()
                   ?? throw new InvalidOperationException("Empty sale response.");
        }

        // ── Generic helpers used by ManagementViewModel ───────────────────────
        public override async Task<List<T>> GetAllAsync<T>(string url)
        {
            var resp = await Http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"GET {url} failed: {resp.StatusCode}");
            return await resp.Content.ReadFromJsonAsync<List<T>>()
                   ?? new List<T>();
        }

        public override async Task PostAsync(string url, object payload)
        {
            var resp = await Http.PostAsJsonAsync(url, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                throw new Exception(msg.Trim('"', ' ') is { Length: > 0 } m ? m : resp.StatusCode.ToString());
            }
        }

        public override async Task PutAsync(string url, object payload)
        {
            var resp = await Http.PutAsJsonAsync(url, payload);
            if (!resp.IsSuccessStatusCode)
            {
                var msg = await resp.Content.ReadAsStringAsync();
                throw new Exception(msg.Trim('"', ' ') is { Length: > 0 } m ? m : resp.StatusCode.ToString());
            }
        }

        // ── Roles ─────────────────────────────────────────────────────────────────
        public override async Task<List<RoleDto>> GetRolesAsync()
        {
            return await Http.GetFromJsonAsync<List<RoleDto>>("api/roles")
                   ?? new List<RoleDto>();
        }

        public override async Task<RoleDto> CreateRoleAsync(CreateRoleRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/roles", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
            return await resp.Content.ReadFromJsonAsync<RoleDto>()
                   ?? throw new InvalidOperationException("Empty role response.");
        }

        public override async Task UpdateRoleAsync(int id, UpdateRoleRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/roles/{id}", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
        }

        public override async Task DeleteRoleAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/roles/{id}");
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
        }
    }
}
