using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.Parts;
using SpareParts.Desktop.Abstractions.UsedCars;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Services.Dialogs;
using SpareParts.Desktop.Wpf.Services.Inventory;
using SpareParts.Desktop.Wpf.Services.UsedCars;
using SpareParts.Desktop.Wpf.ViewModels;

namespace SpareParts.Desktop.Wpf
{
    public partial class App : System.Windows.Application
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
            services.AddSingleton<IArRenderingService, ArRenderingService>();
            services.AddSingleton<IArDeviceBridge, ArDeviceBridge>();

            services.AddTransient<IFilePickerService, FilePickerService>();
            services.AddTransient<IUserNotificationService, UserNotificationService>();
            services.AddTransient<IUsedCarWorkspaceService, UsedCarWorkspaceService>();
            services.AddTransient<IPartWorkspaceService, PartWorkspaceService>();

            services.AddTransient<IAuthApiClient, AuthApiClient>();
            services.AddTransient<IAccountingApiClient, AccountingApiClient>();
            services.AddTransient<IApiSessionClient, ApiSessionClient>();
            services.AddTransient<ICarCatalogApiClient, CarCatalogApiClient>();
            services.AddTransient<ICustomerApiClient, CustomersApiClient>();
            services.AddTransient<IPartsApiClient, PartsApiClient>();
            services.AddTransient<IPurchasesApiClient, PurchasesApiClient>();
            services.AddTransient<IRoleApiClient, RolesApiClient>();
            services.AddTransient<ISalesApiClient, SalesApiClient>();
            services.AddTransient<IUserApiClient, UsersApiClient>();
            services.AddTransient<IWarehouseApiClient, WarehousesApiClient>();
            services.AddTransient<IOwnerCockpitApiClient, OwnerCockpitApiClient>();
            services.AddTransient<IBusinessAssistantApiClient, BusinessAssistantApiClient>();
            services.AddTransient<IGrowthApiClient, GrowthApiClient>();
            services.AddTransient<IReportBuilderApiClient, ReportBuilderApiClient>();
            services.AddTransient<ICrudApiClient, CrudApiClient>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<RolesViewModel>();
            services.AddTransient<ManagementViewModel>();
            services.AddTransient<InvoiceTabsViewModel>();
            services.AddTransient<MainWindow>();
            services.AddTransient<IMainWindowFactory, MainWindowFactory>();
            services.AddTransient<LoginWindow>();
            services.AddTransient<ManagementWindow>();

            return services.BuildServiceProvider();
        }
    }
}
