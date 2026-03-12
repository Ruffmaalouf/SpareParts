using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    // ── View model for a brand tile — wraps CarBrandDto + loaded BitmapImage ──
    public class CarBrandViewModel : INotifyPropertyChanged
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Country     { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public bool   HasLogo     { get; set; }

        private BitmapImage? _logo;
        public BitmapImage? Logo
        {
            get => _logo;
            set { _logo = value; OnPropertyChanged(nameof(Logo)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ── View model for a car model tile ───────────────────────────────────────
    public class CarModelViewModel : INotifyPropertyChanged
    {
        public int     Id         { get; set; }
        public int     CarBrandId { get; set; }
        public string  Name       { get; set; } = string.Empty;
        public string? Year       { get; set; }
        public string? EngineType { get; set; }
        public decimal BasePrice  { get; set; }
        public bool    HasImage   { get; set; }

        private BitmapImage? _image;
        public BitmapImage? Image
        {
            get => _image;
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  InvoiceTabViewModel  — one POS invoice tab
    // ══════════════════════════════════════════════════════════════════════════
    public class InvoiceTabViewModel : INotifyPropertyChanged
    {
        private static int _counter;
        public ObservableCollection<PosItemViewModel> Items { get; } = new();

        public int    TabNumber { get; } = ++_counter;
        public string Header    => $"Invoice #{TabNumber}";

        private int? _customerId;
        public int? CustomerId
        {
            get => _customerId;
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        private int _warehouseId = 1;
        public int WarehouseId
        {
            get => _warehouseId;
            set { _warehouseId = value; OnPropertyChanged(nameof(WarehouseId)); }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set { _paidAmount = value; OnPropertyChanged(nameof(PaidAmount)); OnPropertyChanged(nameof(RemainingAmount)); }
        }

        public decimal TotalAmount     => Items.Sum(i => i.LineTotal);
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        public ICommand SubmitSaleCommand { get; set; } = new RelayCommand(_ => { });

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  InvoiceTabsViewModel  — root DataContext of MainWindow
    // ══════════════════════════════════════════════════════════════════════════
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        // ── Brand / car / part lists ──────────────────────────────────────────
        public ObservableCollection<CarBrandViewModel> GermanBrands   { get; } = new();
        public ObservableCollection<CarBrandViewModel> JapaneseBrands { get; } = new();
        public ObservableCollection<CarBrandViewModel> KoreanBrands   { get; } = new();
        public ObservableCollection<CarModelViewModel> AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel>      AvailableParts { get; } = new();

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

        private bool _isLoadingBrands;
        public bool IsLoadingBrands
        {
            get => _isLoadingBrands;
            set { _isLoadingBrands = value; OnPropertyChanged(nameof(IsLoadingBrands)); }
        }

        // ── Themes ────────────────────────────────────────────────────────────
        public ObservableCollection<ThemeOption> Themes { get; } = new();
        public ICommand SelectThemeCommand { get; private set; } = null!;

        private ThemeOption? _selectedTheme;
        public ThemeOption? SelectedTheme
        {
            get => _selectedTheme;
            set { _selectedTheme = value; OnPropertyChanged(nameof(SelectedTheme)); }
        }

        // ── Tabs ──────────────────────────────────────────────────────────────
        public ObservableCollection<InvoiceTabViewModel> Tabs { get; } = new();

        private InvoiceTabViewModel? _selectedTab;
        public InvoiceTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }

        public ICommand AddTabCommand         { get; }
        public ICommand CloseTabCommand       { get; }
        public ICommand SelectBrandCommand    { get; }
        public ICommand SelectCarCommand      { get; }
        public ICommand SelectPartCommand     { get; }
        public ICommand GoToPosCommand        { get; }
        public ICommand GoToCarSelectionCommand { get; }
        public ICommand GoToHomeCommand       { get; }
        public ICommand OpenManagementCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public InvoiceTabsViewModel()
        {
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
            GoToHomeCommand         = new RelayCommand(_ => { ActiveScreen = PosViewModel.AppScreen.HomePage; SelectedBrand = null; });
            OpenManagementCommand   = new RelayCommand(_ => new ManagementWindow().Show());

            AddTabCommand   = new RelayCommand(_ => AddTab());
            CloseTabCommand = new RelayCommand(tab => CloseTab(tab as InvoiceTabViewModel));

            AddTab();
            _ = LoadBrandsAsync();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  DATA LOADING  — all via ApiClient (no direct DB access)
        // ═════════════════════════════════════════════════════════════════════

        private async Task LoadBrandsAsync()
        {
            IsLoadingBrands = true;
            try
            {
                var dtos = await ApiClient.Instance.GetCarBrandsAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    GermanBrands.Clear();
                    JapaneseBrands.Clear();
                    KoreanBrands.Clear();

                    foreach (var d in dtos)
                    {
                        var vm = new CarBrandViewModel
                        {
                            Id          = d.Id,
                            Name        = d.Name,
                            Country     = d.Country,
                            RegionGroup = d.RegionGroup,
                            HasLogo     = d.HasLogo
                        };

                        switch (d.RegionGroup)
                        {
                            case "German":   GermanBrands.Add(vm);   break;
                            case "Japanese": JapaneseBrands.Add(vm); break;
                            case "Korean":   KoreanBrands.Add(vm);   break;
                        }

                        // Load logo image in background without blocking UI
                        if (d.HasLogo)
                            _ = LoadBrandLogoAsync(vm);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadBrands] {ex.Message}");
            }
            finally
            {
                IsLoadingBrands = false;
            }
        }

        private async Task LoadBrandLogoAsync(CarBrandViewModel vm)
        {
            var bmp = await ApiClient.Instance.GetCarBrandLogoAsync(vm.Id);
            if (bmp == null) return;
            Application.Current.Dispatcher.Invoke(() => vm.Logo = bmp);
        }

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
                    foreach (var d in dtos)
                    {
                        var vm = new CarModelViewModel
                        {
                            Id         = d.Id,
                            CarBrandId = d.CarBrandId,
                            Name       = d.Name,
                            Year       = d.Year,
                            EngineType = d.EngineType,
                            BasePrice  = d.BasePrice,
                            HasImage   = d.HasImage
                        };
                        AvailableCars.Add(vm);

                        if (d.HasImage)
                            _ = LoadCarImageAsync(vm);
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
                    foreach (var d in dtos)
                        AvailableParts.Add(new CarPartModel
                        {
                            PartId      = d.Id,
                            Code        = d.InternalCode,
                            Description = d.Name,
                            UnitPrice   = d.SalePrice
                        });
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadParts] {ex.Message}");
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

        // ── Tab management ────────────────────────────────────────────────────
        private void AddTab()
        {
            var tab = new InvoiceTabViewModel();
            Tabs.Add(tab);
            SelectedTab = tab;
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
