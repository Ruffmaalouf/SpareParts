using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient Http;

        public ApiClient()
        {
            Http = new HttpClient
            {
                BaseAddress = new Uri(AppSettings.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            Http.DefaultRequestHeaders.Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!string.IsNullOrWhiteSpace(ApiClientTokenStore.Token))
            {
                SetToken(ApiClientTokenStore.Token);
            }
        }

        public void SetToken(string token)
        {
            ApiClientTokenStore.Token = token;
            Http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearToken()
        {
            ApiClientTokenStore.Token = null;
            Http.DefaultRequestHeaders.Authorization = null;
        }

        // ── Auth ──────────────────────────────────────────────────────────────
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var resp = await Http.PostAsJsonAsync("api/auth/login",
                new LoginRequest { Username = username, Password = password });

            await ApiClientBase.EnsureSuccessAsync(resp, "Invalid credentials.");

            return await resp.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Empty login response.");
        }

        public async Task<bool> PingAsync()
        {
            try
            {
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(4));
                var response = await Http.GetAsync("api/health", cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    LogWarning($"Ping failed with status code {(int)response.StatusCode}.");
                    return false;
                }

                return true;
            }
            catch (TaskCanceledException ex)
            {
                LogError("Ping timed out.", ex);
                return false;
            }
            catch (Exception ex)
            {
                LogError("Ping failed with unexpected error.", ex);
                return false;
            }
        }

        // ── Users ─────────────────────────────────────────────────────────────
        public async Task<List<UserDto>> GetUsersAsync()
        {
            return await Http.GetFromJsonAsync<List<UserDto>>("api/users")
                   ?? new List<UserDto>();
        }

        public async Task<int> CreateUserAsync(CreateUserRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/users", req);
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
            return await resp.Content.ReadFromJsonAsync<int>();
        }

        public async Task UpdateUserAsync(int id, UpdateUserRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/users/{id}", req);
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
        }

        public async Task DeleteUserAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/users/{id}");
            await ApiClientBase.EnsureSuccessAsync(resp, $"Deactivate failed: {resp.StatusCode}");
        }

        // ── Customers ─────────────────────────────────────────────────────────
        public async Task<List<CustomerDto>> SearchCustomersAsync(string query)
        {
            return await Http.GetFromJsonAsync<List<CustomerDto>>(
                       $"api/customers?search={Uri.EscapeDataString(query)}")
                   ?? new List<CustomerDto>();
        }

        // ── Warehouses ────────────────────────────────────────────────────────
        public async Task<List<WarehouseDto>> GetWarehousesAsync()
        {
            return await Http.GetFromJsonAsync<List<WarehouseDto>>("api/warehouses")
                   ?? new List<WarehouseDto>();
        }

        // ── Car Brands ────────────────────────────────────────────────────────
        public async Task<List<CarBrandDto>> GetCarBrandsAsync()
        {
            return await Http.GetFromJsonAsync<List<CarBrandDto>>("api/carbrands")
                   ?? new List<CarBrandDto>();
        }

        public async Task<BitmapImage?> GetCarBrandLogoAsync(int brandId)
        {
            try
            {
                var resp = await Http.GetAsync($"api/carbrands/{brandId}/logo");
                if (!resp.IsSuccessStatusCode)
                {
                    LogWarning($"Brand logo load failed for brand {brandId}. Status: {(int)resp.StatusCode}.");
                    return null;
                }

                return ApiClientBase.BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch (HttpRequestException ex)
            {
                LogError($"Brand logo request failed for brand {brandId}.", ex);
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Brand logo load failed for brand {brandId}.", ex);
                return null;
            }
        }

        public async Task UploadCarBrandLogoAsync(int brandId, string filePath)
        {
            await using var fs = File.OpenRead(filePath);
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(ApiClientBase.GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await Http.PostAsync($"api/carbrands/{brandId}/logo", content)).EnsureSuccessStatusCode();
        }

        // ── Car Models ────────────────────────────────────────────────────────
        public async Task<List<CarModelDto>> GetCarModelsAsync(int brandId)
        {
            return await Http.GetFromJsonAsync<List<CarModelDto>>(
                       $"api/carmodels?brandId={brandId}")
                   ?? new List<CarModelDto>();
        }

        public async Task<BitmapImage?> GetCarModelImageAsync(int modelId)
        {
            try
            {
                var resp = await Http.GetAsync($"api/carmodels/{modelId}/image");
                if (!resp.IsSuccessStatusCode)
                {
                    LogWarning($"Model image load failed for model {modelId}. Status: {(int)resp.StatusCode}.");
                    return null;
                }

                return ApiClientBase.BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
            }
            catch (HttpRequestException ex)
            {
                LogError($"Model image request failed for model {modelId}.", ex);
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Model image load failed for model {modelId}.", ex);
                return null;
            }
        }

        public async Task UploadCarModelImageAsync(int modelId, string filePath)
        {
            await using var fs = File.OpenRead(filePath);
            var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fs);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(ApiClientBase.GetMimeType(filePath));
            content.Add(fileContent, "image", Path.GetFileName(filePath));
            (await Http.PostAsync($"api/carmodels/{modelId}/image", content)).EnsureSuccessStatusCode();
        }

        // ── Parts ─────────────────────────────────────────────────────────────
        public async Task<List<PartDto>> GetPartsAsync()
        {
            return await Http.GetFromJsonAsync<List<PartDto>>("api/parts")
                   ?? new List<PartDto>();
        }

        // ── Sales ─────────────────────────────────────────────────────────────
        public async Task<CreateSaleResponse> CreateSaleAsync(CreateSaleRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/sales", req);
            await ApiClientBase.EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
            return await resp.Content.ReadFromJsonAsync<CreateSaleResponse>()
                   ?? throw new InvalidOperationException("Empty sale response.");
        }

        public async Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query)
        {
            return await Http.GetFromJsonAsync<List<SalesInvoiceLookupDto>>($"api/sales?search={Uri.EscapeDataString(query ?? string.Empty)}")
                   ?? new List<SalesInvoiceLookupDto>();
        }

        public Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
            => Http.GetFromJsonAsync<SalesInvoiceDetailsDto?>($"api/sales/{invoiceId}");

        // ── Generic helpers used by ManagementViewModel ───────────────────────
        public async Task<List<T>> GetAllAsync<T>(string url)
        {
            var resp = await Http.GetAsync(url);
            await ApiClientBase.EnsureSuccessAsync(resp, $"GET {url} failed: {resp.StatusCode}");
            return await resp.Content.ReadFromJsonAsync<List<T>>()
                   ?? new List<T>();
        }

        public async Task PostAsync(string url, object payload)
        {
            var resp = await Http.PostAsJsonAsync(url, payload);
            await ApiClientBase.EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        public async Task PutAsync(string url, object payload)
        {
            var resp = await Http.PutAsJsonAsync(url, payload);
            await ApiClientBase.EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        public async Task DeleteAsync(string url)
        {
            var resp = await Http.DeleteAsync(url);
            await ApiClientBase.EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        // ── Roles ─────────────────────────────────────────────────────────────────
        public async Task<List<RoleDto>> GetRolesAsync()
        {
            return await Http.GetFromJsonAsync<List<RoleDto>>("api/roles")
                   ?? new List<RoleDto>();
        }

        public async Task<List<RoleMenuAccessDto>> GetRoleMenuAccessAsync(int roleId)
        {
            return await Http.GetFromJsonAsync<List<RoleMenuAccessDto>>($"api/roles/{roleId}/menu-access")
                   ?? new List<RoleMenuAccessDto>();
        }

        public async Task<List<RoleMenuAccessDto>> GetRoleMenuAccessByNameAsync(string roleName)
        {
            var encodedRoleName = Uri.EscapeDataString(roleName ?? string.Empty);
            return await Http.GetFromJsonAsync<List<RoleMenuAccessDto>>($"api/roles/by-name/{encodedRoleName}/menu-access")
                   ?? new List<RoleMenuAccessDto>();
        }

        public async Task UpdateRoleMenuAccessAsync(int roleId, UpdateRoleMenuAccessRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/roles/{roleId}/menu-access", req);
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/roles", req);
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
            return await resp.Content.ReadFromJsonAsync<RoleDto>()
                   ?? throw new InvalidOperationException("Empty role response.");
        }

        public async Task UpdateRoleAsync(int id, UpdateRoleRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/roles/{id}", req);
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
        }

        public async Task DeleteRoleAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/roles/{id}");
            await ApiClientBase.EnsureSuccessAsync(resp, "Request failed.");
        }

        private static void LogWarning(string message)
        {
            Trace.TraceWarning($"[ApiClient] {message}");
        }

        private static void LogError(string message, Exception ex)
        {
            Trace.TraceError($"[ApiClient] {message} Exception: {ex}");
        }
    }
}
