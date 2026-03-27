using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<BrandGroupViewModel> BrandGroups    { get; } = new();
        public ObservableCollection<CarModelViewModel>   AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel>        AvailableParts { get; } = new();
        public ObservableCollection<StatusMessage> Notifications => AppNotificationCenter.Instance.Messages;

        private readonly ICarCatalogApiClient _carCatalogApi;
        private readonly IPartsApiClient _partsApi;
        private readonly ISalesApiClient _salesApi;
        private readonly IRoleApiClient _rolesApi;

        public ManagementViewModel ManagementVm { get; }

        private bool _canViewInvoiceSearch;
        public bool CanViewInvoiceSearch
        {
            get => _canViewInvoiceSearch;
            private set { _canViewInvoiceSearch = value; OnPropertyChanged(nameof(CanViewInvoiceSearch)); }
        }

        private bool _canViewManagementScreen;
        public bool CanViewManagementScreen
        {
            get => _canViewManagementScreen;
            private set { _canViewManagementScreen = value; OnPropertyChanged(nameof(CanViewManagementScreen)); }
        }

        private bool _canViewPosScreen;
        public bool CanViewPosScreen
        {
            get => _canViewPosScreen;
            private set { _canViewPosScreen = value; OnPropertyChanged(nameof(CanViewPosScreen)); }
        }

        private bool _canViewPurchasesScreen;
        public bool CanViewPurchasesScreen
        {
            get => _canViewPurchasesScreen;
            private set { _canViewPurchasesScreen = value; OnPropertyChanged(nameof(CanViewPurchasesScreen)); }
        }

        private bool _canViewStockManagementScreen;
        public bool CanViewStockManagementScreen
        {
            get => _canViewStockManagementScreen;
            private set { _canViewStockManagementScreen = value; OnPropertyChanged(nameof(CanViewStockManagementScreen)); }
        }

        private bool _canViewCarSelectionScreen;
        public bool CanViewCarSelectionScreen
        {
            get => _canViewCarSelectionScreen;
            private set { _canViewCarSelectionScreen = value; OnPropertyChanged(nameof(CanViewCarSelectionScreen)); }
        }

        private bool _canViewPartSelectionScreen;
        public bool CanViewPartSelectionScreen
        {
            get => _canViewPartSelectionScreen;
            private set { _canViewPartSelectionScreen = value; OnPropertyChanged(nameof(CanViewPartSelectionScreen)); }
        }

        private bool _isManagementOpen;
        public bool IsManagementOpen
        {
            get => _isManagementOpen;
            set { _isManagementOpen = value; OnPropertyChanged(nameof(IsManagementOpen)); }
        }

        private bool _isFeedVisible = true;
        public bool IsFeedVisible
        {
            get => _isFeedVisible;
            set
            {
                if (_isFeedVisible == value) return;
                _isFeedVisible = value;
                OnPropertyChanged(nameof(IsFeedVisible));
                OnPropertyChanged(nameof(FeedToggleText));
            }
        }

        public string FeedToggleText => IsFeedVisible ? "Hide" : "Show";

        private CarBrandViewModel? _selectedBrand;
        public CarBrandViewModel? SelectedBrand
        {
            get => _selectedBrand;
            set { _selectedBrand = value; OnPropertyChanged(nameof(SelectedBrand)); }
        }

        private CarModelViewModel? _selectedCar;
        public CarModelViewModel? SelectedCar
        {
            get => _selectedCar;
            set { _selectedCar = value; OnPropertyChanged(nameof(SelectedCar)); }
        }

        private PosViewModel.AppScreen _activeScreen = PosViewModel.AppScreen.HomePage;
        public PosViewModel.AppScreen ActiveScreen
        {
            get => _activeScreen;
            set { if (_activeScreen != value) { _activeScreen = value; OnPropertyChanged(nameof(ActiveScreen)); } }
        }

        private bool _isInvoiceSearchOpen;
        public bool IsInvoiceSearchOpen
        {
            get => _isInvoiceSearchOpen;
            set { _isInvoiceSearchOpen = value; OnPropertyChanged(nameof(IsInvoiceSearchOpen)); }
        }

        private string _invoiceSearchText = string.Empty;
        public string InvoiceSearchText
        {
            get => _invoiceSearchText;
            set
            {
                if (_invoiceSearchText == value) return;
                _invoiceSearchText = value;
                OnPropertyChanged(nameof(InvoiceSearchText));
                RefreshInvoiceSearch();
            }
        }

        public ObservableCollection<SalesInvoiceLookupDto> InvoiceSearchResults { get; } = new();

        private bool _isLoadingInvoiceSearch;
        public bool IsLoadingInvoiceSearch
        {
            get => _isLoadingInvoiceSearch;
            set { _isLoadingInvoiceSearch = value; OnPropertyChanged(nameof(IsLoadingInvoiceSearch)); }
        }

        private bool _isLoadingBrands;
        public bool IsLoadingBrands
        {
            get => _isLoadingBrands;
            set { _isLoadingBrands = value; OnPropertyChanged(nameof(IsLoadingBrands)); }
        }

        public ObservableCollection<ThemeOption> Themes { get; } = new();
        public ICommand SelectThemeCommand { get; private set; } = null!;

        public ObservableCollection<InvoiceTabViewModel> Tabs { get; } = new();
        public ObservableCollection<PurchaseDraftItemViewModel> PurchaseDraftItems { get; } = new();
        public ObservableCollection<StockSnapshotViewModel> StockSnapshots { get; } = new();

        private InvoiceTabViewModel? _selectedTab;
        public InvoiceTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }

        public ICommand AddTabCommand            { get; }
        public ICommand CloseTabCommand          { get; }
        public ICommand SelectBrandCommand       { get; }
        public ICommand SelectCarCommand         { get; }
        public ICommand SelectPartCommand        { get; }
        public ICommand GoToPosCommand           { get; }
        public ICommand GoToCarSelectionCommand  { get; }
        public ICommand GoToHomeCommand          { get; }
        public ICommand OpenManagementCommand    { get; }
        public ICommand OpenInvoiceSearchCommand { get; }
        public ICommand ReloadInvoiceSearchCommand { get; }
        public ICommand GoToPurchasesCommand     { get; }
        public ICommand GoToStockManagementCommand { get; }
        public ICommand ToggleFeedCommand        { get; }

        public InvoiceTabsViewModel(
            IApiClient? apiClient = null,
            ICarCatalogApiClient? carCatalogApi = null,
            IPartsApiClient? partsApi = null,
            ISalesApiClient? salesApi = null,
            ICrudApiClient? crudApi = null)
        {
            var sharedApiClient = apiClient ?? new ApiClient();
            _carCatalogApi = carCatalogApi ?? new CarCatalogApiClient(sharedApiClient);
            _partsApi = partsApi ?? new PartsApiClient(sharedApiClient);
            _salesApi = salesApi ?? new SalesApiClient(sharedApiClient);
            _rolesApi = new RolesApiClient(sharedApiClient);
            ManagementVm = new ManagementViewModel(
                sharedApiClient,
                crudApi ?? new CrudApiClient(sharedApiClient),
                _carCatalogApi);

            Themes.Add(new ThemeOption { Key = AppTheme.Default,       Name = "Default",       SubTitle = "Sport Orange · Dark",       AccentHex = "#FF5722" });
            Themes.Add(new ThemeOption { Key = AppTheme.MPower,        Name = "M Power",       SubTitle = "BMW · Midnight Blue",        AccentHex = "#1C69D4" });
            Themes.Add(new ThemeOption { Key = AppTheme.NeonGlow,      Name = "Neon Glow",     SubTitle = "Cyberpunk · Electric Cyan",  AccentHex = "#00E5FF" });
            Themes.Add(new ThemeOption { Key = AppTheme.AMG,           Name = "AMG",           SubTitle = "Mercedes · Titanium Grey",   AccentHex = "#C0C0C0" });
            Themes.Add(new ThemeOption { Key = AppTheme.PorscheRS,     Name = "Porsche RS",    SubTitle = "Racing · Guards Red",        AccentHex = "#E30613" });
            Themes.Add(new ThemeOption { Key = AppTheme.LamborghiniSC, Name = "Squadra Corse", SubTitle = "Lamborghini · Giallo Orion", AccentHex = "#FFD600" });

            SelectThemeCommand = new RelayCommand(o =>
            {
                if (o is not ThemeOption picked) return;
                foreach (var t in Themes) t.IsSelected = false;
                picked.IsSelected = true;
                ThemeManager.ApplyTheme(picked.Key);
            });
            Themes[0].IsSelected = true;
            ThemeManager.ApplyTheme(AppTheme.Default);

            SelectBrandCommand      = new RelayCommand(SelectBrand);
            SelectCarCommand        = new RelayCommand(SelectCar);
            SelectPartCommand       = new RelayCommand(SelectPart);
            GoToPosCommand          = new RelayCommand(_ =>
            {
                if (!CanViewPosScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the POS screen.", false);
                    return;
                }

                ActiveScreen = PosViewModel.AppScreen.Pos;
            });
            GoToCarSelectionCommand = new RelayCommand(_ =>
            {
                if (!CanViewCarSelectionScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view car selection.", false);
                    return;
                }

                ActiveScreen = PosViewModel.AppScreen.CarSelection;
            });
            GoToHomeCommand         = new RelayCommand(_ =>
            {
                ActiveScreen  = PosViewModel.AppScreen.HomePage;
                SelectedBrand = null;
                AvailableCars.Clear();
            });

            OpenManagementCommand = new RelayCommand(_ =>
            {
                if (!CanViewManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the management screen.", false);
                    return;
                }

                IsManagementOpen = !IsManagementOpen;
                if (IsManagementOpen)
                    _ = ManagementVm.LoadAllAsync();
            });

            OpenInvoiceSearchCommand = new RelayCommand(_ =>
            {
                if (!CanViewInvoiceSearch)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view invoice search.", false);
                    return;
                }

                IsInvoiceSearchOpen = !IsInvoiceSearchOpen;
                if (IsInvoiceSearchOpen)
                {
                    RefreshInvoiceSearch();
                }
            });
            ReloadInvoiceSearchCommand = new RelayCommand(_ => RefreshInvoiceSearch());

            GoToPurchasesCommand = new RelayCommand(_ =>
            {
                if (!CanViewPurchasesScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the purchases screen.", false);
                    return;
                }

                ActiveScreen = PosViewModel.AppScreen.Purchases;
            });
            GoToStockManagementCommand = new RelayCommand(_ =>
            {
                if (!CanViewStockManagementScreen)
                {
                    AppNotificationCenter.Instance.Publish("✗ You do not have permission to view stock management.", false);
                    return;
                }

                ActiveScreen = PosViewModel.AppScreen.StockManagement;
            });
            ToggleFeedCommand = new RelayCommand(_ => IsFeedVisible = !IsFeedVisible);
            AddTabCommand = new RelayCommand(_ => AddTab());
            CloseTabCommand = new RelayCommand(o => CloseTab(o as InvoiceTabViewModel));

           // SeedPurchasesAndStock();
            AddTab();
            RefreshInvoiceSearch();
            _ = LoadBrandsAsync();
            _ = LoadRolePermissionsAsync();
        }

        private async Task LoadRolePermissionsAsync()
        {
            try
            {
                var roleName = SessionContext.CurrentUser?.Role;
                if (string.IsNullOrWhiteSpace(roleName))
                {
                    ApplyPermissions(new List<RoleMenuAccessDto>());
                    return;
                }

                var permissions = await _rolesApi.GetRoleMenuAccessByNameAsync(roleName);
                ApplyPermissions(permissions);
            }
            catch (Exception)
            {
                ApplyPermissions(new List<RoleMenuAccessDto>());
                AppNotificationCenter.Instance.Publish("✗ Could not load role permissions. Access is restricted.", false);
            }
        }

        private void ApplyPermissions(List<RoleMenuAccessDto> menuAccessItems)
        {
            var invoiceSearch = GetMenuAccess(menuAccessItems, "invoice_search");
            var managementScreen = GetMenuAccess(menuAccessItems, "management_screen");
            var supplierTab = GetMenuAccess(menuAccessItems, "supplier_tab");
            var posScreen = GetMenuAccess(menuAccessItems, "pos_screen");
            var purchasesScreen = GetMenuAccess(menuAccessItems, "purchases_screen");
            var stockManagementScreen = GetMenuAccess(menuAccessItems, "stock_management_screen");
            var carSelectionScreen = GetMenuAccess(menuAccessItems, "car_selection_screen");
            var partSelectionScreen = GetMenuAccess(menuAccessItems, "part_selection_screen");

            CanViewInvoiceSearch = invoiceSearch.CanView;
            CanViewManagementScreen = managementScreen.CanView;
            CanViewPosScreen = posScreen.CanView;
            CanViewPurchasesScreen = purchasesScreen.CanView;
            CanViewStockManagementScreen = stockManagementScreen.CanView;
            CanViewCarSelectionScreen = carSelectionScreen.CanView;
            CanViewPartSelectionScreen = partSelectionScreen.CanView;
            ManagementVm.SetSupplierPermissions(
                supplierTab.CanView,
                supplierTab.CanEdit,
                supplierTab.CanModify,
                supplierTab.CanDelete);
        }

        private static RoleMenuAccessDto GetMenuAccess(IEnumerable<RoleMenuAccessDto> menuAccessItems, string menuKey)
            => menuAccessItems.FirstOrDefault(i => string.Equals(i.MenuKey, menuKey, StringComparison.OrdinalIgnoreCase))
               ?? new RoleMenuAccessDto { MenuKey = menuKey };


        private void RefreshInvoiceSearch()
        {
            _ = RefreshInvoiceSearchAsync();
        }

        private async Task RefreshInvoiceSearchAsync()
        {
            try
            {
                IsLoadingInvoiceSearch = true;
                var results = await _salesApi.SearchInvoicesAsync(InvoiceSearchText ?? string.Empty);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    InvoiceSearchResults.Clear();
                    foreach (var invoice in results)
                    {
                        InvoiceSearchResults.Add(invoice);
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while searching invoices.", false);
            }
            finally
            {
                IsLoadingInvoiceSearch = false;
            }
        }

        public async Task OpenInvoiceFromSearchAsync(SalesInvoiceLookupDto? lookup)
        {
            if (lookup == null)
            {
                return;
            }

            var existing = Tabs.FirstOrDefault(t => t.InvoiceId == lookup.InvoiceId);
            if (existing != null)
            {
                SelectedTab = existing;
                ActiveScreen = PosViewModel.AppScreen.Pos;
                IsInvoiceSearchOpen = false;
                return;
            }

            try
            {
                var invoice = await _salesApi.GetInvoiceByIdAsync(lookup.InvoiceId);
                if (invoice == null)
                {
                    AppNotificationCenter.Instance.Publish("✗ Selected invoice was not found.", false);
                    return;
                }

                var tab = new InvoiceTabViewModel(_salesApi);
                tab.LoadFromDatabase(invoice);
                Tabs.Add(tab);
                SelectedTab = tab;
                ActiveScreen = PosViewModel.AppScreen.Pos;
                IsInvoiceSearchOpen = false;
                AppNotificationCenter.Instance.Publish($"✓ Loaded invoice {invoice.InvoiceNumber} from database.", true);
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while opening invoice.", false);
            }
        }

        private async Task LoadBrandsAsync()
        {
            IsLoadingBrands = true;
            try
            {
                var dtos = await _carCatalogApi.GetCarBrandsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BrandGroups.Clear();
                    var knownOrder = new[] { "German", "Japanese", "Korean" };
                    var grouped = dtos
                        .GroupBy(d => d.RegionGroup)
                        .OrderBy(g => { int i = Array.IndexOf(knownOrder, g.Key); return i >= 0 ? i : 99; });

                    foreach (var grp in grouped)
                    {
                        var groupVm = new BrandGroupViewModel { RegionGroup = grp.Key.ToUpperInvariant() };
                        foreach (var b in grp.OrderBy(x => x.SortOrder).ThenBy(x => x.Name))
                        {
                            var bvm = new CarBrandViewModel
                            {
                                Id          = b.Id,
                                Name        = b.Name,
                                Country     = b.Country,
                                RegionGroup = b.RegionGroup
                            };
                            groupVm.Brands.Add(bvm);
                            if (b.HasLogo) _ = LoadLogoAsync(bvm);
                        }
                        BrandGroups.Add(groupVm);
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading brands.", false);
            }
            finally { IsLoadingBrands = false; }
        }

        private async Task LoadLogoAsync(CarBrandViewModel vm)
        {
            var bmp = await _carCatalogApi.GetCarBrandLogoAsync(vm.Id);
            if (bmp == null) return;
            Application.Current.Dispatcher.Invoke(() => vm.Logo = bmp);
        }

        private void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrandViewModel brand) return;
            if (!CanViewCarSelectionScreen)
            {
                AppNotificationCenter.Instance.Publish("✗ You do not have permission to view car selection.", false);
                return;
            }

            SelectedBrand = brand;
            ActiveScreen  = PosViewModel.AppScreen.CarSelection;
            _ = LoadCarsAsync(brand.Id);
        }

        private async Task LoadCarsAsync(int brandId)
        {
            AvailableCars.Clear();
            try
            {
                var dtos = await _carCatalogApi.GetCarModelsAsync(brandId);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableCars.Clear();
                    foreach (var dto in dtos)
                    {
                        var vm = new CarModelViewModel
                        {
                            Id         = dto.Id,
                            CarBrandId = dto.CarBrandId,
                            Name       = dto.Name,
                            Year       = dto.Year,
                            EngineType = dto.EngineType,
                            BasePrice  = dto.BasePrice,
                            HasImage   = dto.HasImage
                        };
                        AvailableCars.Add(vm);
                        if (dto.HasImage) _ = LoadCarImageAsync(vm);
                    }
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading car models.", false);
            }
        }

        private async Task LoadCarImageAsync(CarModelViewModel vm)
        {
            var bmp = await _carCatalogApi.GetCarModelImageAsync(vm.Id);
            if (bmp == null) return;
            Application.Current.Dispatcher.Invoke(() => vm.Image = bmp);
        }

        private void SelectCar(object? parameter)
        {
            if (parameter is not CarModelViewModel car) return;
            if (!CanViewPartSelectionScreen)
            {
                AppNotificationCenter.Instance.Publish("✗ You do not have permission to view part selection.", false);
                return;
            }

            SelectedCar  = car;
            ActiveScreen = PosViewModel.AppScreen.PartSelection;
            _ = LoadPartsAsync();
        }

        private async Task LoadPartsAsync()
        {
            AvailableParts.Clear();
            try
            {
                var dtos = await _partsApi.GetPartsAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableParts.Clear();
                    foreach (var dto in dtos)
                        AvailableParts.Add(new CarPartModel
                        {
                            PartId      = dto.Id,
                            Code        = dto.InternalCode,
                            Description = dto.Name,
                            UnitPrice   = dto.SalePrice
                        });
                });
            }
            catch (ApiClientException ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while loading parts.", false);
            }
        }

        private void SelectPart(object? parameter)
        {
            if (parameter is not CarPartModel part || SelectedTab == null) return;
            SelectedTab.Items.Add(new PosItemViewModel
            {
                PartId      = part.PartId,
                Description = part.Description,
                Quantity    = 1,
                UnitPrice   = part.UnitPrice
            });
            ActiveScreen = PosViewModel.AppScreen.Pos;
        }

        private void AddTab()
        {
            var tab = new InvoiceTabViewModel(_salesApi);
            Tabs.Add(tab);
            SelectedTab = tab;
            RefreshInvoiceSearch();
        }

        private void CloseTab(InvoiceTabViewModel? tab)
        {
            if (tab == null || Tabs.Count <= 1) return;
            int idx = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            SelectedTab = Tabs[Math.Max(0, idx - 1)];
            RefreshInvoiceSearch();
        }



       

        public void OpenInvoiceFromSearch(InvoiceTabViewModel? tab)
        {
            if (tab == null)
            {
                return;
            }

            SelectedTab = tab;
            ActiveScreen = PosViewModel.AppScreen.Pos;
            tab.MarkOpenedFromSearch();
            IsInvoiceSearchOpen = false;
            AppNotificationCenter.Instance.Publish($"✓ Opened {tab.Header} for editing.", true);
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
