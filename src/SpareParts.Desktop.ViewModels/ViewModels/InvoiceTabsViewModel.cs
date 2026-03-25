using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<BrandGroupViewModel> BrandGroups    { get; } = new();
        public ObservableCollection<CarModelViewModel>   AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel>        AvailableParts { get; } = new();
        public ObservableCollection<StatusMessage> Notifications => AppNotificationCenter.Instance.Messages;

        // ── Management panel ──────────────────────────────────────────────────
        private readonly ICarCatalogApiClient _carCatalogApi;
        private readonly IPartsApiClient _partsApi;
        private readonly ISalesApiClient _salesApi;

        public ManagementViewModel ManagementVm { get; }

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

        // ── Selected items ────────────────────────────────────────────────────
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

        // ── Active screen ─────────────────────────────────────────────────────
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

        public ObservableCollection<InvoiceTabViewModel> InvoiceSearchResults { get; } = new();

        private bool _isLoadingBrands;
        public bool IsLoadingBrands
        {
            get => _isLoadingBrands;
            set { _isLoadingBrands = value; OnPropertyChanged(nameof(IsLoadingBrands)); }
        }

        // ── Themes ────────────────────────────────────────────────────────────
        public ObservableCollection<ThemeOption> Themes { get; } = new();
        public ICommand SelectThemeCommand { get; private set; } = null!;

        // ── Invoice tabs ──────────────────────────────────────────────────────
        public ObservableCollection<InvoiceTabViewModel> Tabs { get; } = new();

        private InvoiceTabViewModel? _selectedTab;
        public InvoiceTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand AddTabCommand           { get; }
        public ICommand CloseTabCommand         { get; }
        public ICommand SelectBrandCommand      { get; }
        public ICommand SelectCarCommand        { get; }
        public ICommand SelectPartCommand       { get; }
        public ICommand GoToPosCommand          { get; }
        public ICommand GoToCarSelectionCommand { get; }
        public ICommand GoToHomeCommand         { get; }
        public ICommand OpenManagementCommand   { get; }
        public ICommand OpenInvoiceSearchCommand { get; }
        public ICommand ToggleFeedCommand       { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public InvoiceTabsViewModel(ICarCatalogApiClient? carCatalogApi = null, IPartsApiClient? partsApi = null, ISalesApiClient? salesApi = null, ICrudApiClient? crudApi = null)
        {
            _carCatalogApi = carCatalogApi ?? new CarCatalogApiClient();
            _partsApi = partsApi ?? new PartsApiClient();
            _salesApi = salesApi ?? new SalesApiClient();
            ManagementVm = new ManagementViewModel(crudApi ?? new CrudApiClient(), _carCatalogApi);

            // Themes
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
            GoToPosCommand          = new RelayCommand(_ => ActiveScreen = PosViewModel.AppScreen.Pos);
            GoToCarSelectionCommand = new RelayCommand(_ => ActiveScreen = PosViewModel.AppScreen.CarSelection);
            GoToHomeCommand         = new RelayCommand(_ =>
            {
                ActiveScreen  = PosViewModel.AppScreen.HomePage;
                SelectedBrand = null;
                AvailableCars.Clear();
            });

            // Toggle immediately (sync) then kick off load as fire-and-forget.
            // Using plain RelayCommand keeps CanExecute always true so the button
            // never gets disabled while data loads.
            OpenManagementCommand = new RelayCommand(_ =>
            {
                IsManagementOpen = !IsManagementOpen;
                if (IsManagementOpen)
                    _ = ManagementVm.LoadAllAsync();
            });
            OpenInvoiceSearchCommand = new RelayCommand(_ =>
            {
                IsInvoiceSearchOpen = !IsInvoiceSearchOpen;
                if (IsInvoiceSearchOpen)
                {
                    RefreshInvoiceSearch();
                }
            });
            ToggleFeedCommand = new RelayCommand(_ => IsFeedVisible = !IsFeedVisible);

            AddTabCommand   = new RelayCommand(_ => AddTab());
            CloseTabCommand = new RelayCommand(o => CloseTab(o as InvoiceTabViewModel));

            AddTab();
            RefreshInvoiceSearch();
            _ = LoadBrandsAsync();
        }

        // ── Data loading ──────────────────────────────────────────────────────
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

        // ── Brand selected ────────────────────────────────────────────────────
        private void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrandViewModel brand) return;
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

        // ── Car selected ──────────────────────────────────────────────────────
        private void SelectCar(object? parameter)
        {
            if (parameter is not CarModelViewModel car) return;
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

        // ── Part selected ─────────────────────────────────────────────────────
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

        // ── Tab management ────────────────────────────────────────────────────
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



        private void RefreshInvoiceSearch()
        {
            var query = (InvoiceSearchText ?? string.Empty).Trim();
            var matches = Tabs
                .Where(t => string.IsNullOrWhiteSpace(query)
                            || t.Header.Contains(query, StringComparison.OrdinalIgnoreCase)
                            || t.Items.Any(i => i.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                            || t.Items.Any(i => i.PartId.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            InvoiceSearchResults.Clear();
            foreach (var tab in matches)
            {
                InvoiceSearchResults.Add(tab);
            }
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
