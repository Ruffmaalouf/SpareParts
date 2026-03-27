using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiSessionClient : IApiSessionClient
    {
        private readonly IApiClient _api;

        public ApiSessionClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public void SetToken(string token) => _api.SetToken(token);

        public void ClearToken() => _api.ClearToken();
    }
}
