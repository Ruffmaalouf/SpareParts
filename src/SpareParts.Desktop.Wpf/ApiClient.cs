using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
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
    public class ApiClient
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static readonly ApiClient Instance = new();

        private readonly HttpClient _http;

        private ApiClient()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri(AppSettings.ApiBaseUrl),
                Timeout     = TimeSpan.FromSeconds(30)
            };
            _http.DefaultRequestHeaders.Accept
                 .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ── Token management ──────────────────────────────────────────────────
        public void SetToken(string token)
            => _http.DefaultRequestHeaders.Authorization =
                   new AuthenticationHeaderValue("Bearer", token);

        public void ClearToken()
            => _http.DefaultRequestHeaders.Authorization = null;

        // ── Auth ──────────────────────────────────────────────────────────────
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var resp = await _http.PostAsJsonAsync("api/auth/login",
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

        public async Task<bool> PingAsync()
        {
            try
            {
                using var cts  = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
                await _http.GetAsync("api/health", cts.Token);
                return true;
            }
            catch { return false; }
        }

        // ── Users ─────────────────────────────────────────────────────────────
        public async Task<List<UserManagementDto>> GetUsersAsync()
        {
            return await _http.GetFromJsonAsync<List<UserManagementDto>>("api/users")
                   ?? new List<UserManagementDto>();
        }

        public async Task<int> CreateUserAsync(CreateUserRequest req)
        {
            var resp = await _http.PostAsJsonAsync("api/users", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
            return await resp.Content.ReadFromJsonAsync<int>();
        }

        public async Task UpdateUserAsync(int id, UpdateUserRequest req)
        {
            var resp = await _http.PutAsJsonAsync($"api/users/{id}", req);
            if (!resp.IsSuccessStatusCode)
                throw new Exception((await resp.Content.ReadAsStringAsync()).Trim('"'));
        }

        public async Task DeleteUserAsync(int id)
        {
            var resp = await _http.DeleteAsync($"api/users/{id}");
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Deactivate failed: {resp.StatusCode}");
        }

        // ── Customers ─────────────────────────────────────────────────────────
        public async Task<List<CustomerDto>> SearchCustomersAsync(string query)
        {
            return await _http.GetFromJsonAsync<List<CustomerDto>>(
                       $"api/customers?search={Uri.EscapeDataString(query)}")
                   ?? new List<CustomerDto>();
        }

        // ── Car Brands ────────────────────────────────────────────────────────
        public async Task<List<CarBrandDto>> GetCarBrandsAsync()
        {
            return await _http.GetFromJsonAsync<List<CarBrandDto>>("api/carbrands")
                   ?? new List<CarBrandDto>();
        }

        public async Task<BitmapImage?> GetCarBrandLogoAsync(int brandId)
        {
            try
            {
                var resp = await _http.GetAsync($"api/carbrands/{brandId}/logo");
                if (!resp.IsSuccessStatusCode) return null;
                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch { return null; }
        }

        public async Task UploadCarBrandLogoAsync(int brandId, string filePath)
        {
            await using var fs      = File.OpenRead(filePath);
            var content             = new MultipartFormDataContent();
            var fileContent         = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await _http.PostAsync($"api/carbrands/{brandId}/logo", content)).EnsureSuccessStatusCode();
        }

        // ── Car Models ────────────────────────────────────────────────────────
        public async Task<List<CarModelDto>> GetCarModelsAsync(int brandId)
        {
            return await _http.GetFromJsonAsync<List<CarModelDto>>(
                       $"api/carmodels?brandId={brandId}")
                   ?? new List<CarModelDto>();
        }

        public async Task<BitmapImage?> GetCarModelImageAsync(int modelId)
        {
            try
            {
                var resp = await _http.GetAsync($"api/carmodels/{modelId}/image");
                if (!resp.IsSuccessStatusCode) return null;
                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch { return null; }
        }

        public async Task UploadCarModelImageAsync(int modelId, string filePath)
        {
            await using var fs      = File.OpenRead(filePath);
            var content             = new MultipartFormDataContent();
            var fileContent         = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await _http.PostAsync($"api/carmodels/{modelId}/image", content)).EnsureSuccessStatusCode();
        }

        // ── Parts ─────────────────────────────────────────────────────────────
        public async Task<List<PartDto>> GetPartsAsync()
        {
            return await _http.GetFromJsonAsync<List<PartDto>>("api/parts")
                   ?? new List<PartDto>();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static BitmapImage BytesToBitmap(byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            var bmp      = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption  = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private static string GetMimeType(string path) =>
            Path.GetExtension(path).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp"           => "image/webp",
                _                 => "image/png"
            };
    }
}
