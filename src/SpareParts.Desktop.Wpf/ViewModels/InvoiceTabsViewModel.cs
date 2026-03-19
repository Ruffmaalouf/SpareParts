using SpareParts.Desktop.Wpf.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<BrandGroupViewModel>                         BrandGroups   { get; } = new();
        public ObservableCollection<CarModelViewModel>                           AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel>                                AvailableParts { get; } = new();
        public ObservableCollection<SpareParts.Domain.MasterData.WarehouseDto>  Warehouses     { get; } = new();

        // ── Management panel (embedded in MainWindow) ─────────────────────────
        public ManagementViewModel ManagementVm { get; } = new();

        private bool _isManagementOpen;
        public bool IsManagementOpen
        {
            get => _isManagementOpen;
            set { _isManagementOpen = value; OnPropertyChanged(nameof(IsManagementOpen)); }
        }

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
            set
            {
                if (_activeScreen != value)
                {
                    _activeScreen = value;
                    OnPropertyChanged(nameof(ActiveScreen));
                }
            }
        }

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
        public ICommand OpenManagementCommand   { get; }  // kept same name so XAML binding unchanged

        // ── Constructor ───────────────────────────────────────────────────────
        public InvoiceTabsViewModel()
        {
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

            // Toggle the embedded management panel — no separate window
            OpenManagementCommand = new RelayCommand(async _ =>
            {
                IsManagementOpen = !IsManagementOpen;
                if (IsManagementOpen)
                    await ManagementVm.LoadAllAsync();
            });

            AddTabCommand   = new RelayCommand(_ => AddTab());
            CloseTabCommand = new RelayCommand(o => CloseTab(o as InvoiceTabViewModel));

            AddTab();
            _ = LoadBrandsAsync();
            _ = LoadWarehousesAsync();
        }

        private async Task LoadWarehousesAsync()
        {
            try
            {
                var list = await ApiClient.Instance.GetWarehousesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Warehouses.Clear();
                    foreach (var w in list) Warehouses.Add(w);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadWarehouses] {ex.Message}");
            }
        }

        // ── Data loading ──────────────────────────────────────────────────────
        private async Task LoadBrandsAsync()
        {
            IsLoadingBrands = true;
            try
            {
                var dtos = await ApiClient.Instance.GetCarBrandsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    BrandGroups.Clear();

                    var knownOrder = new[] { "German", "Japanese", "Korean" };
                    var grouped = dtos
                        .GroupBy(d => d.RegionGroup)
                        .OrderBy(g =>
                        {
                            int i = Array.IndexOf(knownOrder, g.Key);
                            return i >= 0 ? i : 99;
                        });

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
                            if (b.HasLogo)
                                _ = LoadLogoAsync(bvm);
                        }
                        BrandGroups.Add(groupVm);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadBrands] {ex.Message}");
            }
            finally { IsLoadingBrands = false; }
        }

        private async Task LoadLogoAsync(CarBrandViewModel vm)
        {
            var bmp = await ApiClient.Instance.GetCarBrandLogoAsync(vm.Id);
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
                var dtos = await ApiClient.Instance.GetCarModelsAsync(brandId);
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadCars] {ex.Message}");
            }
        }

        private async Task LoadCarImageAsync(CarModelViewModel vm)
        {
            var bmp = await ApiClient.Instance.GetCarModelImageAsync(vm.Id);
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
                var dtos = await ApiClient.Instance.GetPartsAsync();
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadParts] {ex.Message}");
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
            var tab = new InvoiceTabViewModel();
            Tabs.Add(tab);
            SelectedTab = tab;
            _ = LoadWarehousesIntoTab(tab);
        }

        private async Task LoadWarehousesIntoTab(InvoiceTabViewModel tab)
        {
            //try
            //{
            //    var warehouses = await ApiClient.Instance.GetWarehousesAsync();
            //    Application.Current.Dispatcher.Invoke(() =>
            //    {
            //        tab.Warehouses.Clear();
            //        foreach (var w in warehouses) tab.Warehouses.Add(w);
            //        if (tab.Warehouses.Count > 0)
            //            tab.SelectedWarehouse = tab.Warehouses[0];
            //    });
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine($"[LoadWarehouses] {ex.Message}");
            //}
        }

        private void CloseTab(InvoiceTabViewModel? tab)
        {
            if (tab == null || Tabs.Count <= 1) return;
            int idx = Tabs.IndexOf(tab);
            Tabs.Remove(tab);
            SelectedTab = Tabs[Math.Max(0, idx - 1)];
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
