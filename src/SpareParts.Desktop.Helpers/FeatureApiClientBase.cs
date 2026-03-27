using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public abstract class FeatureApiClientBase
    {
        protected readonly RestClient Client;

        protected FeatureApiClientBase(string baseUrl)
        {
            Client = new RestClient(new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false 
            });
        }

        protected RestRequest CreateRequest(string resource, Method method)
        {
            var request = new RestRequest(resource, method);
            if (!string.IsNullOrWhiteSpace(ApiClientTokenStore.Token))
            {
                request.AddOrUpdateHeader("Authorization", $"Bearer {ApiClientTokenStore.Token}");
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

        protected async Task<TResponse> RetrieveOneAsync<TResponse>(string resource, string emptyMessage)
        {
            var request = CreateRequest(resource, Method.Get);
            var response = await Client.ExecuteAsync<TResponse>(request);
            ApiClientBase.EnsureSuccess(response, $"GET {resource} failed.");
            return response.Data ?? throw new InvalidOperationException(emptyMessage);
        }

        protected async Task AddAsync(string resource, object payload)
        {
            var request = CreateRequest(resource, Method.Post).AddJsonBody(payload);
            var response = await Client.ExecuteAsync(request);
            ApiClientBase.EnsureSuccess(response, $"POST {resource} failed.");
        }

        protected async Task<TResponse> AddAsync<TResponse>(string resource, object payload, string emptyMessage)
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
            ApiClientTokenStore.Token = token;
        }

        protected void ClearTokenInternal()
        {
            ApiClientTokenStore.Token = null;
        }
    }
}
