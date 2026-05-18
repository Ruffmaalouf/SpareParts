using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiSessionClient : FeatureApiClientBase, IApiSessionClient
    {
        public ApiSessionClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.IdentityApiBaseUrl)
        {
        }

        public void SetToken(string token) => SetTokenInternal(token);

        public void ClearToken() => ClearTokenInternal();
    }
}
