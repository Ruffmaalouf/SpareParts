using SpareParts.Desktop.Wpf.Interfaces;

namespace SpareParts.Desktop.Wpf
{
    public sealed class ApiSessionClient : FeatureApiClientBase, IApiSessionClient
    {
        public ApiSessionClient(IApiClient? api = null) : base(AppSettings.ApiBaseUrl)
        {
        }

        public void SetToken(string token) => SetTokenInternal(token);

        public void ClearToken() => ClearTokenInternal();
    }
}
