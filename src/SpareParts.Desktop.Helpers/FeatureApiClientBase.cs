using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
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
            _tokenProvider.Token = token;
        }

        protected void ClearTokenInternal()
        {
            _tokenProvider.Clear();
        }
    }
}
