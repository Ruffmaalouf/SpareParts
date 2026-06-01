using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public abstract class FeatureApiClientBase
    {
        protected readonly RestClient Client;
        private readonly IApiTokenProvider _tokenProvider;

        protected FeatureApiClientBase(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider, string baseUrl)
        {
            Client = restClientFactory.Create(baseUrl);
            _tokenProvider = tokenProvider;
        }

        protected RestRequest CreateRequest(string resource, Method method)
        {
            var request = new RestRequest(resource, method);
            if (!string.IsNullOrWhiteSpace(_tokenProvider.Token))
            {
                request.AddOrUpdateHeader("Authorization", $"Bearer {_tokenProvider.Token}");
            }

            return request;
        }

        protected async Task<List<T>> RetrieveAsync<T>(string resource)
        {
            var request = CreateRequest(resource, Method.Get);
            var response = await Client.ExecuteAsync<List<T>>(request);
            ApiClientBase.EnsureSuccess(response, $"GET {resource} failed.");
            return response.Data ?? new List<T>();
        }

        protected async Task<List<T>> RetrieveAllPagesAsync<T>(string resource, int pageSize = 5000)
        {
            var rows = new List<T>();
            var page = 1;

            while (true)
            {
                var pagedResource = AppendPagination(resource, page, pageSize);
                var request = CreateRequest(pagedResource, Method.Get);
                var response = await Client.ExecuteAsync<List<T>>(request);
                ApiClientBase.EnsureSuccess(response, $"GET {pagedResource} failed.");

                var batch = response.Data ?? new List<T>();
                rows.AddRange(batch);

                var totalCount = ReadHeaderInt(response, "X-Total-Count");
                var effectivePageSize = ReadHeaderInt(response, "X-Page-Size") ?? pageSize;
                if (batch.Count == 0
                    || batch.Count < effectivePageSize
                    || (totalCount.HasValue && rows.Count >= totalCount.Value))
                {
                    return rows;
                }

                page++;
            }
        }

        protected async Task<TResponse> RetrieveOneAsync<TResponse>(string resource, string emptyMessage)
            where TResponse : notnull
        {
            var request = CreateRequest(resource, Method.Get);
            var response = await Client.ExecuteAsync<TResponse>(request);
            ApiClientBase.EnsureSuccess(response, $"GET {resource} failed.");
            return response.Data ?? throw new InvalidOperationException(emptyMessage);
        }

        protected async Task<TResponse?> RetrieveOptionalAsync<TResponse>(string resource)
            where TResponse : class
        {
            var request = CreateRequest(resource, Method.Get);
            var response = await Client.ExecuteAsync<TResponse>(request);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            ApiClientBase.EnsureSuccess(response, $"GET {resource} failed.");
            return response.Data;
        }

        protected async Task AddAsync(string resource, object payload)
        {
            var request = CreateRequest(resource, Method.Post).AddJsonBody(payload);
            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"POST {resource} failed.");
        }

        protected async Task<TResponse> AddAsync<TResponse>(string resource, object payload, string emptyMessage)
            where TResponse : notnull
        {
            var request = CreateRequest(resource, Method.Post).AddJsonBody(payload);
            var response = await Client.ExecuteAsync<TResponse>(request);
            ApiClientBase.EnsureSuccess(response, $"POST {resource} failed.");
            return response.Data ?? throw new InvalidOperationException(emptyMessage);
        }

        protected async Task EditAsync(string resource, object payload)
        {
            var request = CreateRequest(resource, Method.Put).AddJsonBody(payload);
            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"PUT {resource} failed.");
        }

        protected async Task DeleteResourceAsync(string resource)
        {
            var request = CreateRequest(resource, Method.Delete);
            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"DELETE {resource} failed.");
        }

        protected void SetTokenInternal(string token)
        {
            _tokenProvider.Token = token;
        }

        protected void ClearTokenInternal()
        {
            _tokenProvider.Clear();
        }

        private static string AppendPagination(string resource, int page, int pageSize)
        {
            var separator = resource.Contains("?", StringComparison.Ordinal) ? "&" : "?";
            return $"{resource}{separator}page={page}&pageSize={pageSize}";
        }

        private static int? ReadHeaderInt(RestResponse response, string name)
        {
            var value = response.Headers?
                .FirstOrDefault(header => string.Equals(header.Name?.ToString(), name, StringComparison.OrdinalIgnoreCase))?
                .Value?
                .ToString();

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }
    }
}
