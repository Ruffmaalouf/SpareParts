using SpareParts.Desktop.Wpf.Interfaces;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class App : Application
    {
        private readonly IApiClient _apiClient = new ApiClient();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var loginWindow = new LoginWindow(_apiClient);
            loginWindow.Show();
        }
    }
}
