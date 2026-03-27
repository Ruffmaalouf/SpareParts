using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.ViewModels;

namespace SpareParts.Desktop.Wpf
{
    public partial class App : Application
    {
        public static ServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Services = ConfigureServices();
            ServiceLocator.Provider = Services;

            var loginWindow = Services.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private static ServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            services.AddSingleton<IApiTokenProvider, ApiTokenProvider>();
            services.AddSingleton<IRestClientFactory, RestClientFactory>();

            services.AddTransient<IAuthApiClient, AuthApiClient>();
            services.AddTransient<IApiSessionClient, ApiSessionClient>();
            services.AddTransient<ICarCatalogApiClient, CarCatalogApiClient>();
            services.AddTransient<ICustomerApiClient, CustomersApiClient>();
            services.AddTransient<IPartsApiClient, PartsApiClient>();
            services.AddTransient<IRoleApiClient, RolesApiClient>();
            services.AddTransient<ISalesApiClient, SalesApiClient>();
            services.AddTransient<IUserApiClient, UsersApiClient>();
            services.AddTransient<IWarehouseApiClient, WarehousesApiClient>();
            services.AddTransient<ICrudApiClient, CrudApiClient>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<RolesViewModel>();
            services.AddTransient<ManagementViewModel>();
            services.AddTransient<InvoiceTabsViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<ManagementWindow>();

            return services.BuildServiceProvider();
        }
    }
}
