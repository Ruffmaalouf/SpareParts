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
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
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
        public abstract Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query);
        public abstract Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId);
        public abstract Task<List<T>> GetAllAsync<T>(string url);
        public abstract Task PostAsync(string url, object payload);
        public abstract Task PutAsync(string url, object payload);
        public abstract Task DeleteAsync(string url);
        public abstract Task<List<RoleDto>> GetRolesAsync();
        public abstract Task<List<RoleMenuAccessDto>> GetRoleMenuAccessAsync(int roleId);
        public abstract Task<List<RoleMenuAccessDto>> GetRoleMenuAccessByNameAsync(string roleName);
        public abstract Task UpdateRoleMenuAccessAsync(int roleId, UpdateRoleMenuAccessRequest req);
        public abstract Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        public abstract Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        public abstract Task DeleteRoleAsync(int id);


        protected static async Task EnsureSuccessAsync(HttpResponseMessage response, string fallbackMessage)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var raw = await response.Content.ReadAsStringAsync();
            try
            {
                var envelope = JsonSerializer.Deserialize<ApiErrorEnvelope>(raw, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope != null && !string.IsNullOrWhiteSpace(envelope.Code))
                {
                    throw new ApiClientException(envelope.Code, envelope.Message, envelope.TraceId);
                }
            }
            catch (JsonException)
            {
                // ignore and fallback below
            }

            throw new ApiClientException("http_error", string.IsNullOrWhiteSpace(raw) ? fallbackMessage : raw.Trim('"', ' ', '\n'));
        }
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
}
