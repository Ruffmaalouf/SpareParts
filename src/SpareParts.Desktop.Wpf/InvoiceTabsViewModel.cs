using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    // ══════════════════════════════════════════════════════════════════════════
    // InvoiceTabViewModel
    // Owns ONLY the per-invoice data: line items, customer, warehouse, payment.
    // ══════════════════════════════════════════════════════════════════════════
    public class InvoiceTabViewModel : INotifyPropertyChanged
    {
        private static int _counter = 1;

        private string _header;
        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }

        public ObservableCollection<PosItemViewModel> Items { get; } = new();

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
            set
            {
                _paidAmount = value;
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            }
        }

        public decimal TotalAmount     => Items.Sum(i => i.LineTotal);
        public decimal RemainingAmount => PaidAmount - TotalAmount;

        public ICommand SubmitSaleCommand { get; }

        public InvoiceTabViewModel()
        {
            _header = $"Invoice #{_counter++}";
            SubmitSaleCommand = new RelayCommand(_ => SubmitSale());
            Items.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            };
        }

        private void SubmitSale()
        {
            if (!Items.Any())
            {
                CustomMessageBox.Show("No items in invoice.", "Validation Error", "Warning");
                return;
            }

            try
            {
                var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000/") };

                var req = new CreateSaleRequest
                {
                    InvoiceDate   = DateTime.Now,
                    CustomerId    = this.CustomerId,
                    WarehouseId   = this.WarehouseId,
                    PaymentMethod = "Cash",
                    PaidAmount    = this.PaidAmount,
                    Notes         = "WPF POS Sale"
                };

                foreach (var item in Items)
                    req.Items.Add(new SaleItemDto
                    {
                        PartId = item.PartId, Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice, DiscountAmount = 0, TaxRate = 0
                    });

                var json     = JsonSerializer.Serialize(req);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/sales", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    CustomMessageBox.Show($"API error: {response.StatusCode}", "Error", "Warning");
                    return;
                }

                var respJson = response.Content.ReadAsStringAsync().Result;
                var result   = JsonSerializer.Deserialize<CreateSaleResponse>(respJson,
                                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                CustomMessageBox.Show(
                    $"Sale created.\nInvoice: {result?.InvoiceNumber}\nTotal: {result?.TotalAmount}",
                    "Success", "Info");

                Items.Clear();
                PaidAmount = 0;
                Header = $"Invoice #{_counter++}";
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error: {ex.Message}", "Error", "Warning");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    // ══════════════════════════════════════════════════════════════════════════
    // InvoiceTabsViewModel  ← root DataContext of MainWindow
    // ══════════════════════════════════════════════════════════════════════════
    public class InvoiceTabsViewModel : INotifyPropertyChanged
    {
        // ── Brand / car / part lists ──────────────────────────────────────────
        public ObservableCollection<CarBrand>     GermanBrands   { get; } = new();
        public ObservableCollection<CarBrand>     JapaneseBrands { get; } = new();
        public ObservableCollection<CarBrand>     KoreanBrands   { get; } = new();
        public ObservableCollection<CarModel>     AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel> AvailableParts { get; } = new();

        private CarBrand? _selectedBrand;
        public CarBrand? SelectedBrand
        {
            get => _selectedBrand;
            set { _selectedBrand = value; OnPropertyChanged(nameof(SelectedBrand)); }
        }

        private CarModel? _selectedCar;
        public CarModel? SelectedCar
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

        // ── Themes ────────────────────────────────────────────────────────────
        public ObservableCollection<ThemeOption> Themes { get; } = new();
        public ICommand SelectThemeCommand { get; private set; } = null!;

        private ThemeOption? _selectedTheme;
        public ThemeOption? SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnPropertyChanged(nameof(SelectedTheme));
                    if (value != null) ThemeManager.ApplyTheme(value.Key);
                }
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand SelectBrandCommand      { get; }
        public ICommand SelectCarCommand        { get; }
        public ICommand SelectPartCommand       { get; }
        public ICommand GoToPosCommand          { get; }
        public ICommand GoToCarSelectionCommand { get; }
        public ICommand GoToHomeCommand         { get; }
        public ICommand OpenManagementCommand   { get; }

        // ── Tab strip ─────────────────────────────────────────────────────────
        public ObservableCollection<InvoiceTabViewModel> Tabs { get; } = new();

        private InvoiceTabViewModel? _selectedTab;
        public InvoiceTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set { _selectedTab = value; OnPropertyChanged(nameof(SelectedTab)); }
        }

        public ICommand AddTabCommand   { get; }
        public ICommand CloseTabCommand { get; }

        // ── Constructor ───────────────────────────────────────────────────────
        public InvoiceTabsViewModel()
        {
            Themes.Add(new ThemeOption { Key = AppTheme.Default,       Name = "Default",                    SubTitle = "Sport Orange · Dark",      AccentHex = "#FF5722" });
            Themes.Add(new ThemeOption { Key = AppTheme.MPower,        Name = "M Power",                    SubTitle = "BMW · Midnight Blue",       AccentHex = "#1C69D4" });
            Themes.Add(new ThemeOption { Key = AppTheme.NeonGlow,      Name = "Neon Glow",                  SubTitle = "Cyberpunk · Electric Cyan", AccentHex = "#00E5FF" });
            Themes.Add(new ThemeOption { Key = AppTheme.AMG,           Name = "AMG",                        SubTitle = "Mercedes · Titanium Grey",  AccentHex = "#C0C0C0" });
            Themes.Add(new ThemeOption { Key = AppTheme.PorscheRS,     Name = "Porsche RS",                 SubTitle = "Racing · Guards Red",       AccentHex = "#E30613" });
            Themes.Add(new ThemeOption { Key = AppTheme.LamborghiniSC, Name = "Squadra Corse",              SubTitle = "Lamborghini · Giallo Orion", AccentHex = "#FFD600" });

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

            SeedBrands();
            AddTab();
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

        // ── Brand → car → part flow ───────────────────────────────────────────
        private void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrand brand) return;
            SelectedBrand = brand;
            LoadAvailableCars(brand.Name);
            ActiveScreen = PosViewModel.AppScreen.CarSelection;
        }

        private void SelectCar(object? parameter)
        {
            if (parameter is not CarModel car) return;
            SelectedCar = car;
            LoadAvailableParts(car);
            ActiveScreen = PosViewModel.AppScreen.PartSelection;
        }

        private void SelectPart(object? parameter)
        {
            if (parameter is not CarPartModel part) return;
            if (SelectedTab == null) return;
            SelectedTab.Items.Add(new PosItemViewModel
            {
                PartId      = part.PartId,
                Description = part.Description,
                Quantity    = 1,
                UnitPrice   = part.UnitPrice
            });

            ActiveScreen = PosViewModel.AppScreen.Pos;
        }

        // ── Brand seeding ─────────────────────────────────────────────────────
        private void SeedBrands()
        {
            GermanBrands.Add(new CarBrand { Name = "BMW",           Country = "Germany", LogoPath = "Assets/Logos/bmw.png" });
            GermanBrands.Add(new CarBrand { Name = "Mercedes-Benz", Country = "Germany", LogoPath = "Assets/Logos/mercedes.png" });
            GermanBrands.Add(new CarBrand { Name = "Audi",          Country = "Germany", LogoPath = "Assets/Logos/audi.png" });
            GermanBrands.Add(new CarBrand { Name = "Volkswagen",    Country = "Germany", LogoPath = "Assets/Logos/volkswagen.png" });
            GermanBrands.Add(new CarBrand { Name = "Porsche",       Country = "Germany", LogoPath = "Assets/Logos/porsche.png" });
            GermanBrands.Add(new CarBrand { Name = "Opel",          Country = "Germany", LogoPath = "Assets/Logos/opel.png" });

            JapaneseBrands.Add(new CarBrand { Name = "Toyota", Country = "Japan", LogoPath = "Assets/Logos/toyota.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Honda",  Country = "Japan", LogoPath = "Assets/Logos/honda.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Nissan", Country = "Japan", LogoPath = "Assets/Logos/nissan.png" });

            KoreanBrands.Add(new CarBrand { Name = "Hyundai", Country = "Korea", LogoPath = "Assets/Logos/hyundai.png" });
            KoreanBrands.Add(new CarBrand { Name = "Kia",     Country = "Korea", LogoPath = "Assets/Logos/kia.png" });
        }

        // ── Car seeding ───────────────────────────────────────────────────────
        private void LoadAvailableCars(string? brandName)
        {
            AvailableCars.Clear();
            switch (brandName)
            {
                case "BMW":
                    AvailableCars.Add(new CarModel { ModelId = 101, Name = "M3 Competition", Year = "2024", EngineType = "S58 I6",      BasePrice = 85000,  ImagePath = "Assets/Cars/bmw_m3.png" });
                    AvailableCars.Add(new CarModel { ModelId = 102, Name = "i4 M50",          Year = "2023", EngineType = "Electric",    BasePrice = 69000,  ImagePath = "Assets/Cars/bmw_i4.png" });
                    AvailableCars.Add(new CarModel { ModelId = 103, Name = "X5 40i",          Year = "2023", EngineType = "B58 I6",      BasePrice = 73000,  ImagePath = "Assets/Cars/bmw_x5.png" });
                    AvailableCars.Add(new CarModel { ModelId = 104, Name = "320i",             Year = "2022", EngineType = "B48 I4",      BasePrice = 42000,  ImagePath = "Assets/Cars/bmw_320i.png" });
                    AvailableCars.Add(new CarModel { ModelId = 105, Name = "M5 CS",           Year = "2022", EngineType = "S63 V8",      BasePrice = 120000, ImagePath = "Assets/Cars/bmw_m5cs.png" });
                    break;
                case "Mercedes-Benz":
                    AvailableCars.Add(new CarModel { ModelId = 201, Name = "C200",    Year = "2023", EngineType = "M254 I4",     BasePrice = 45000, ImagePath = "Assets/Cars/merc_c200.png" });
                    AvailableCars.Add(new CarModel { ModelId = 202, Name = "E300",    Year = "2023", EngineType = "M264 I4",     BasePrice = 53000, ImagePath = "Assets/Cars/merc_e300.png" });
                    AvailableCars.Add(new CarModel { ModelId = 203, Name = "GLC300",  Year = "2024", EngineType = "M254 I4",     BasePrice = 51000, ImagePath = "Assets/Cars/merc_glc.png" });
                    AvailableCars.Add(new CarModel { ModelId = 204, Name = "AMG C63", Year = "2024", EngineType = "M139 Hybrid", BasePrice = 92000, ImagePath = "Assets/Cars/merc_c63.png" });
                    break;
                case "Audi":
                    AvailableCars.Add(new CarModel { ModelId = 301, Name = "A4 45 TFSI", Year = "2023", EngineType = "2.0T I4", BasePrice = 44000,  ImagePath = "Assets/Cars/audi_a4.png" });
                    AvailableCars.Add(new CarModel { ModelId = 302, Name = "A6 55 TFSI", Year = "2023", EngineType = "3.0T V6", BasePrice = 56000,  ImagePath = "Assets/Cars/audi_a6.png" });
                    AvailableCars.Add(new CarModel { ModelId = 303, Name = "Q7 55 TFSI", Year = "2023", EngineType = "3.0T V6", BasePrice = 59000,  ImagePath = "Assets/Cars/audi_q7.png" });
                    AvailableCars.Add(new CarModel { ModelId = 304, Name = "RS6 Avant",  Year = "2022", EngineType = "4.0T V8", BasePrice = 115000, ImagePath = "Assets/Cars/audi_rs6.png" });
                    break;
                case "Toyota":
                    AvailableCars.Add(new CarModel { ModelId = 401, Name = "Corolla",     Year = "2024", EngineType = "1.6L I4",     BasePrice = 21000, ImagePath = "Assets/Cars/toyota_corolla.png" });
                    AvailableCars.Add(new CarModel { ModelId = 402, Name = "Camry",        Year = "2024", EngineType = "2.5L I4",     BasePrice = 28000, ImagePath = "Assets/Cars/toyota_camry.png" });
                    AvailableCars.Add(new CarModel { ModelId = 403, Name = "RAV4",         Year = "2024", EngineType = "2.5L Hybrid", BasePrice = 33000, ImagePath = "Assets/Cars/toyota_rav4.png" });
                    AvailableCars.Add(new CarModel { ModelId = 404, Name = "Land Cruiser", Year = "2023", EngineType = "3.5L TT V6",  BasePrice = 95000, ImagePath = "Assets/Cars/toyota_landcruiser.png" });
                    break;
                case "Honda":
                    AvailableCars.Add(new CarModel { ModelId = 501, Name = "Civic",  Year = "2024", EngineType = "1.5T I4",     BasePrice = 24000, ImagePath = "Assets/Cars/honda_civic.png" });
                    AvailableCars.Add(new CarModel { ModelId = 502, Name = "Accord", Year = "2024", EngineType = "1.5T I4",     BasePrice = 29000, ImagePath = "Assets/Cars/honda_accord.png" });
                    AvailableCars.Add(new CarModel { ModelId = 503, Name = "CR-V",   Year = "2024", EngineType = "2.0 Hybrid",  BasePrice = 35000, ImagePath = "Assets/Cars/honda_crv.png" });
                    AvailableCars.Add(new CarModel { ModelId = 504, Name = "Type R", Year = "2023", EngineType = "K20C1 Turbo", BasePrice = 43000, ImagePath = "Assets/Cars/honda_typr.png" });
                    break;
                case "Nissan":
                    AvailableCars.Add(new CarModel { ModelId = 601, Name = "Altima",   Year = "2023", EngineType = "2.5L I4",    BasePrice = 26000, ImagePath = "Assets/Cars/nissan_altima.png" });
                    AvailableCars.Add(new CarModel { ModelId = 602, Name = "Patrol",   Year = "2023", EngineType = "5.6L V8",    BasePrice = 82000, ImagePath = "Assets/Cars/nissan_patrol.png" });
                    AvailableCars.Add(new CarModel { ModelId = 603, Name = "X-Trail",  Year = "2024", EngineType = "2.5L I4",    BasePrice = 32000, ImagePath = "Assets/Cars/nissan_xtrail.png" });
                    AvailableCars.Add(new CarModel { ModelId = 604, Name = "Nissan Z", Year = "2023", EngineType = "3.0L TT V6", BasePrice = 43000, ImagePath = "Assets/Cars/nissan_z.png" });
                    break;
                case "Hyundai":
                    AvailableCars.Add(new CarModel { ModelId = 701, Name = "Tucson",   Year = "2024", EngineType = "2.0L I4", BasePrice = 28000, ImagePath = "Assets/Cars/hyundai_tucson.png" });
                    AvailableCars.Add(new CarModel { ModelId = 702, Name = "Elantra",  Year = "2024", EngineType = "2.0L I4", BasePrice = 22000, ImagePath = "Assets/Cars/hyundai_elantra.png" });
                    AvailableCars.Add(new CarModel { ModelId = 703, Name = "Santa Fe", Year = "2023", EngineType = "2.5T I4", BasePrice = 38000, ImagePath = "Assets/Cars/hyundai_santafe.png" });
                    break;
                case "Kia":
                    AvailableCars.Add(new CarModel { ModelId = 801, Name = "Sportage", Year = "2024", EngineType = "2.0L I4", BasePrice = 27000, ImagePath = "Assets/Cars/kia_sportage.png" });
                    AvailableCars.Add(new CarModel { ModelId = 802, Name = "Cerato",   Year = "2024", EngineType = "2.0L I4", BasePrice = 21000, ImagePath = "Assets/Cars/kia_cerato.png" });
                    AvailableCars.Add(new CarModel { ModelId = 803, Name = "Sorento",  Year = "2023", EngineType = "2.5T I4", BasePrice = 36000, ImagePath = "Assets/Cars/kia_sorento.png" });
                    break;
                case "Volkswagen":
                    AvailableCars.Add(new CarModel { ModelId = 901, Name = "Golf GTI", Year = "2024", EngineType = "2.0T I4", BasePrice = 34000, ImagePath = "Assets/Cars/vw_golf.png" });
                    AvailableCars.Add(new CarModel { ModelId = 902, Name = "Tiguan",   Year = "2024", EngineType = "2.0T I4", BasePrice = 38000, ImagePath = "Assets/Cars/vw_tiguan.png" });
                    break;
                case "Porsche":
                    AvailableCars.Add(new CarModel { ModelId = 1001, Name = "911 Carrera", Year = "2024", EngineType = "3.0T H6", BasePrice = 115000, ImagePath = "Assets/Cars/porsche_911.png" });
                    AvailableCars.Add(new CarModel { ModelId = 1002, Name = "Cayenne",     Year = "2024", EngineType = "3.0T V6", BasePrice = 85000,  ImagePath = "Assets/Cars/porsche_cayenne.png" });
                    break;
                case "Opel":
                    AvailableCars.Add(new CarModel { ModelId = 1101, Name = "Astra",    Year = "2023", EngineType = "1.4T I4", BasePrice = 22000, ImagePath = "Assets/Cars/opel_astra.png" });
                    AvailableCars.Add(new CarModel { ModelId = 1102, Name = "Insignia", Year = "2023", EngineType = "2.0T I4", BasePrice = 30000, ImagePath = "Assets/Cars/opel_insignia.png" });
                    break;
            }
        }

        // ── Parts seeding ─────────────────────────────────────────────────────
        private void LoadAvailableParts(CarModel car)
        {
            AvailableParts.Clear();
            switch (car.ModelId)
            {
                case 101:
                    AvailableParts.Add(new CarPartModel { PartId = 10001, Code = "BMW-M3-OILFLT",   Description = "Oil Filter - M3 Competition",   UnitPrice = 45 });
                    AvailableParts.Add(new CarPartModel { PartId = 10002, Code = "BMW-M3-AIRFLT",   Description = "Air Filter - High Flow",         UnitPrice = 70 });
                    AvailableParts.Add(new CarPartModel { PartId = 10003, Code = "BMW-M3-BRKPAD-F", Description = "Front Brake Pads - Performance", UnitPrice = 220 });
                    AvailableParts.Add(new CarPartModel { PartId = 10004, Code = "BMW-M3-SPARK",    Description = "Spark Plugs Set",                UnitPrice = 160 });
                    break;
                case 104:
                    AvailableParts.Add(new CarPartModel { PartId = 11001, Code = "BMW-F30-OILFLT",   Description = "Oil Filter - 320i", UnitPrice = 35 });
                    AvailableParts.Add(new CarPartModel { PartId = 11002, Code = "BMW-F30-BRKPAD-F", Description = "Front Brake Pads",  UnitPrice = 150 });
                    AvailableParts.Add(new CarPartModel { PartId = 11003, Code = "BMW-F30-BRKPAD-R", Description = "Rear Brake Pads",   UnitPrice = 130 });
                    break;
                default:
                    AvailableParts.Add(new CarPartModel { PartId = 90001, Code = "GEN-OIL-5W40",   Description = "Engine Oil 5W-40 (4L)",  UnitPrice = 35 });
                    AvailableParts.Add(new CarPartModel { PartId = 90002, Code = "GEN-BRKPAD-SET", Description = "Brake Pads Set (Front)", UnitPrice = 120 });
                    AvailableParts.Add(new CarPartModel { PartId = 90003, Code = "GEN-AIRFLT",     Description = "Air Filter - Standard",  UnitPrice = 22 });
                    AvailableParts.Add(new CarPartModel { PartId = 90004, Code = "GEN-SPARKPLUG",  Description = "Spark Plugs Set",        UnitPrice = 55 });
                    AvailableParts.Add(new CarPartModel { PartId = 90005, Code = "GEN-CABINFLT",   Description = "Cabin Air Filter",       UnitPrice = 18 });
                    break;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
