using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class CrudApiClient : ICrudApiClient
    {
        private readonly IRestClientFactory _restClientFactory;
        private readonly IApiTokenProvider _tokenProvider;

        public CrudApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
        {
            _restClientFactory = restClientFactory;
            _tokenProvider = tokenProvider;
        }

        public async Task<List<T>> GetAllAsync<T>(string url)
        {
            var client = CreateClientForResource(url);
            var request = CreateRequest(url, Method.Get);
            var response = await client.ExecuteAsync<List<T>>(request);
            ApiClientBase.EnsureSuccess(response, $"GET {url} failed.");
            return response.Data ?? new List<T>();
        }

        public async Task PostAsync(string url, object payload)
        {
            var client = CreateClientForResource(url);
            var request = CreateRequest(url, Method.Post).AddJsonBody(payload);
            var response = await client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"POST {url} failed.");
        }

        public async Task<TResponse> PostAsync<TResponse>(string url, object payload)
            where TResponse : notnull
        {
            var client = CreateClientForResource(url);
            var request = CreateRequest(url, Method.Post).AddJsonBody(payload);
            var response = await client.ExecuteAsync<TResponse>(request);
            ApiClientBase.EnsureSuccess(response, $"POST {url} failed.");
            return response.Data ?? throw new InvalidOperationException($"POST {url} returned an empty response.");
        }

        public async Task PutAsync(string url, object payload)
        {
            var client = CreateClientForResource(url);
            var request = CreateRequest(url, Method.Put).AddJsonBody(payload);
            var response = await client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"PUT {url} failed.");
        }

        public async Task DeleteAsync(string url)
        {
            var client = CreateClientForResource(url);
            var request = CreateRequest(url, Method.Delete);
            var response = await client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"DELETE {url} failed.");
        }

        private RestClient CreateClientForResource(string resource)
            => _restClientFactory.Create(ResolveBaseUrl(resource));

        private RestRequest CreateRequest(string resource, Method method)
        {
            var request = new RestRequest(resource, method);
            if (!string.IsNullOrWhiteSpace(_tokenProvider.Token))
            {
                request.AddOrUpdateHeader("Authorization", $"Bearer {_tokenProvider.Token}");
            }

            return request;
        }

        private static string ResolveBaseUrl(string resource)
        {
            var route = resource.TrimStart('/').ToLowerInvariant();

            if (route.StartsWith("api/customers") || route.StartsWith("api/sales") || route.StartsWith("api/loyalty") || route.StartsWith("api/warranty") || route.StartsWith("api/shipments") || route.StartsWith("api/customer-pricing"))
                return AppSettings.SalesApiBaseUrl;

            if (route.StartsWith("api/suppliers") || route.StartsWith("api/purchases") || route.StartsWith("api/supplier-price-history"))
                return AppSettings.PurchasesApiBaseUrl;

            if (route.StartsWith("api/parts") || route.StartsWith("api/partrequests") || route.StartsWith("api/warehouses") || route.StartsWith("api/transactiontypes") || route.StartsWith("api/scans") || route.StartsWith("api/reorder"))
                return AppSettings.InventoryApiBaseUrl;

            if (route.StartsWith("api/users") || route.StartsWith("api/roles") || route.StartsWith("api/auth"))
                return AppSettings.IdentityApiBaseUrl;

            if (route.StartsWith("api/brands")
                || route.StartsWith("api/categories")
                || route.StartsWith("api/carbrands")
                || route.StartsWith("api/carmodels")
                || route.StartsWith("api/locations")
                || route.StartsWith("api/usedcars")
                || route.StartsWith("api/currencies")
                || route.StartsWith("api/appconstants")
                || route.StartsWith("api/excelimport"))
                return AppSettings.CatalogApiBaseUrl;

            if (route.StartsWith("api/reportbuilder"))
                return AppSettings.ApiBaseUrl;

            if (route.StartsWith("api/communications"))
                return AppSettings.ApiBaseUrl;

            return AppSettings.ApiBaseUrl;
        }
    }
}
