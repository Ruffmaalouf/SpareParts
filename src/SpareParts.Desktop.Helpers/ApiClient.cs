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

            await EnsureSuccessAsync(resp, "Invalid credentials.");

            return await resp.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Empty login response.");
        }

        public override async Task<bool> PingAsync()
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
        public override async Task<List<UserDto>> GetUsersAsync()
        {
            return await Http.GetFromJsonAsync<List<UserDto>>("api/users")
                   ?? new List<UserDto>();
        }

        public override async Task<int> CreateUserAsync(CreateUserRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/users", req);
            await EnsureSuccessAsync(resp, "Request failed.");
            return await resp.Content.ReadFromJsonAsync<int>();
        }

        public override async Task UpdateUserAsync(int id, UpdateUserRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/users/{id}", req);
            await EnsureSuccessAsync(resp, "Request failed.");
        }

        public override async Task DeleteUserAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/users/{id}");
            await EnsureSuccessAsync(resp, $"Deactivate failed: {resp.StatusCode}");
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
                if (!resp.IsSuccessStatusCode)
                {
                    LogWarning($"Brand logo load failed for brand {brandId}. Status: {(int)resp.StatusCode}.");
                    return null;
                }

                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
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
                if (!resp.IsSuccessStatusCode)
                {
                    LogWarning($"Model image load failed for model {modelId}. Status: {(int)resp.StatusCode}.");
                    return null;
                }

                return BytesToBitmap(await resp.Content.ReadAsByteArrayAsync());
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
            await EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
            return await resp.Content.ReadFromJsonAsync<CreateSaleResponse>()
                   ?? throw new InvalidOperationException("Empty sale response.");
        }

        public override async Task<List<SalesInvoiceLookupDto>> SearchInvoicesAsync(string query)
        {
            return await Http.GetFromJsonAsync<List<SalesInvoiceLookupDto>>($"api/sales?search={Uri.EscapeDataString(query ?? string.Empty)}")
                   ?? new List<SalesInvoiceLookupDto>();
        }

        public override Task<SalesInvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
            => Http.GetFromJsonAsync<SalesInvoiceDetailsDto?>($"api/sales/{invoiceId}");

        // ── Generic helpers used by ManagementViewModel ───────────────────────
        public override async Task<List<T>> GetAllAsync<T>(string url)
        {
            var resp = await Http.GetAsync(url);
            await EnsureSuccessAsync(resp, $"GET {url} failed: {resp.StatusCode}");
            return await resp.Content.ReadFromJsonAsync<List<T>>()
                   ?? new List<T>();
        }

        public override async Task PostAsync(string url, object payload)
        {
            var resp = await Http.PostAsJsonAsync(url, payload);
            await EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        public override async Task PutAsync(string url, object payload)
        {
            var resp = await Http.PutAsJsonAsync(url, payload);
            await EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        public override async Task DeleteAsync(string url)
        {
            var resp = await Http.DeleteAsync(url);
            await EnsureSuccessAsync(resp, $"Request failed: {resp.StatusCode}");
        }

        // ── Roles ─────────────────────────────────────────────────────────────────
        public override async Task<List<RoleDto>> GetRolesAsync()
        {
            return await Http.GetFromJsonAsync<List<RoleDto>>("api/roles")
                   ?? new List<RoleDto>();
        }

        public override async Task<List<RoleMenuAccessDto>> GetRoleMenuAccessAsync(int roleId)
        {
            return await Http.GetFromJsonAsync<List<RoleMenuAccessDto>>($"api/roles/{roleId}/menu-access")
                   ?? new List<RoleMenuAccessDto>();
        }

        public override async Task<List<RoleMenuAccessDto>> GetRoleMenuAccessByNameAsync(string roleName)
        {
            var encodedRoleName = Uri.EscapeDataString(roleName ?? string.Empty);
            return await Http.GetFromJsonAsync<List<RoleMenuAccessDto>>($"api/roles/by-name/{encodedRoleName}/menu-access")
                   ?? new List<RoleMenuAccessDto>();
        }

        public override async Task UpdateRoleMenuAccessAsync(int roleId, UpdateRoleMenuAccessRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/roles/{roleId}/menu-access", req);
            await EnsureSuccessAsync(resp, "Request failed.");
        }

        public override async Task<RoleDto> CreateRoleAsync(CreateRoleRequest req)
        {
            var resp = await Http.PostAsJsonAsync("api/roles", req);
            await EnsureSuccessAsync(resp, "Request failed.");
            return await resp.Content.ReadFromJsonAsync<RoleDto>()
                   ?? throw new InvalidOperationException("Empty role response.");
        }

        public override async Task UpdateRoleAsync(int id, UpdateRoleRequest req)
        {
            var resp = await Http.PutAsJsonAsync($"api/roles/{id}", req);
            await EnsureSuccessAsync(resp, "Request failed.");
        }

        public override async Task DeleteRoleAsync(int id)
        {
            var resp = await Http.DeleteAsync($"api/roles/{id}");
            await EnsureSuccessAsync(resp, "Request failed.");
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
