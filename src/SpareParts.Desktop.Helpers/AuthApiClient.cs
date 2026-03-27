using RestSharp;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class AuthApiClient : FeatureApiClientBase, IAuthApiClient
    {
        public AuthApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<LoginResponse> LoginAsync(string username, string password)
            => AddAsync<LoginResponse>(
                "api/auth/login",
                new LoginRequest { Username = username, Password = password },
                "Empty login response.");

        public async Task<bool> PingAsync()
        {
            try
            {
                var request = CreateRequest("api/health", Method.Get);
                request.Timeout = TimeSpan.FromSeconds(4);
                var response = await Client.ExecuteAsync(request);
                return response.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }
    }
}
