using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class AuthApiClient : IAuthApiClient
    {
        private readonly IApiClient _api;

        public AuthApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<LoginResponse> LoginAsync(string username, string password) => _api.LoginAsync(username, password);

        public Task<bool> PingAsync() => _api.PingAsync();
    }
}
