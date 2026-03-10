using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
namespace SpareParts.Desktop.Wpf
{
    public class PosItemViewModel : INotifyPropertyChanged
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    // --- New Model (Add this class) ---
    public class CarModel
    {
        public int ModelId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string EngineType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string ImagePath { get; set; } = string.Empty; // e.g., Assets/Cars/m3.png
    }
    // ---------------------------------
    public class CarBrand
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty; // Germany / Japan
        public string LogoPath { get; set; } = string.Empty; // relative path, e.g. Assets/Logos/bmw.png
    }

    // DTOs must match API DTOs
    public class SaleItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
    }
    public class CarPartModel
    {
        public int PartId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
    public class CreateSaleRequest
    {
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal PaidAmount { get; set; }
        public System.Collections.Generic.List<SaleItemDto> Items { get; set; } = new System.Collections.Generic.List<SaleItemDto>();
        public string? Notes { get; set; }
    }

    public class CreateSaleResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class PosViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PosItemViewModel> Items { get; } = new ObservableCollection<PosItemViewModel>();

        // Brand lists
        public ObservableCollection<CarBrand> GermanBrands { get; } = new ObservableCollection<CarBrand>();
        public ObservableCollection<CarBrand> JapaneseBrands { get; } = new ObservableCollection<CarBrand>();
        public ObservableCollection<CarBrand> KoreanBrands { get; } = new ObservableCollection<CarBrand>();

        private CarBrand? _selectedBrand;
        public CarBrand? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                _selectedBrand = value;
                OnPropertyChanged(nameof(SelectedBrand));
            }
        }
        private CarModel? _selectedCar;
        public CarModel? SelectedCar
        {
            get => _selectedCar;
            set
            {
                _selectedCar = value;
                OnPropertyChanged(nameof(SelectedCar));
            }
        }

        private ObservableCollection<CarPartModel> _availableParts = new();
        public ObservableCollection<CarPartModel> AvailableParts
        {
            get => _availableParts;
            set
            {
                _availableParts = value;
                OnPropertyChanged(nameof(AvailableParts));
            }
        }
        public enum AppScreen
        {
            Pos,
            CarSelection,
            PartSelection,
            HomePage
        }

        private AppScreen _activeScreen = AppScreen.HomePage;
        public AppScreen ActiveScreen
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
        private ObservableCollection<CarModel> _availableCars = new ObservableCollection<CarModel>();
        public ObservableCollection<CarModel> AvailableCars
        {
            get => _availableCars;
            set
            {
                _availableCars = value;
                OnPropertyChanged(nameof(AvailableCars));
            }
        }

        // --- New Commands ---
        public ICommand GoToPosCommand { get; }
        public ICommand SelectCarCommand { get; }

        public ICommand GoToCarSelectionCommand { get; }
        public ICommand SelectPartCommand { get; }
        public ICommand GoToHomeCommand { get; }

        // --- Updated Command Logic ---

        private void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrand brand)
                return;

            SelectedBrand = brand;
            OnPropertyChanged(nameof(SelectedBrand));

            // 1. Load cars for that brand
            LoadAvailableCars(brand.Name);

            // 2. Switch to car selection screen
            ActiveScreen = AppScreen.CarSelection;
        }

        private void LoadAvailableCars(string? brandName)
        {
            AvailableCars.Clear();
            if (brandName == "BMW")
            {
                AvailableCars.Add(new CarModel { ModelId = 101, Name = "M3 Competition", Year = "2024", EngineType = "S58 I6", BasePrice = 85000, ImagePath = "Assets/Cars/bmw_m3.png" });
                AvailableCars.Add(new CarModel { ModelId = 102, Name = "i4 M50", Year = "2023", EngineType = "Electric", BasePrice = 69000, ImagePath = "Assets/Cars/bmw_i4.png" });
                AvailableCars.Add(new CarModel { ModelId = 103, Name = "X5 40i", Year = "2023", EngineType = "B58 I6", BasePrice = 73000, ImagePath = "Assets/Cars/bmw_x5.png" });
                AvailableCars.Add(new CarModel { ModelId = 104, Name = "320i", Year = "2022", EngineType = "B48 I4", BasePrice = 42000, ImagePath = "Assets/Cars/bmw_320i.png" });
                AvailableCars.Add(new CarModel { ModelId = 105, Name = "M5 CS", Year = "2022", EngineType = "S63 V8", BasePrice = 120000, ImagePath = "Assets/Cars/bmw_m5cs.png" });
            }
            else if (brandName == "Nissan")
            {
                AvailableCars.Add(new CarModel { ModelId = 601, Name = "Altima", Year = "2023", EngineType = "2.5L I4", BasePrice = 26000, ImagePath = "Assets/Cars/nissan_altima.png" });
                AvailableCars.Add(new CarModel { ModelId = 602, Name = "Patrol", Year = "2023", EngineType = "5.6L V8", BasePrice = 82000, ImagePath = "Assets/Cars/nissan_patrol.png" });
                AvailableCars.Add(new CarModel { ModelId = 603, Name = "X-Trail", Year = "2024", EngineType = "2.5L I4", BasePrice = 32000, ImagePath = "Assets/Cars/nissan_xtrail.png" });
                AvailableCars.Add(new CarModel { ModelId = 604, Name = "Nissan Z", Year = "2023", EngineType = "3.0L TT V6", BasePrice = 43000, ImagePath = "Assets/Cars/nissan_z.png" });
            }
            else if (brandName == "Honda")
            {
                AvailableCars.Add(new CarModel { ModelId = 501, Name = "Civic", Year = "2024", EngineType = "1.5T I4", BasePrice = 24000, ImagePath = "Assets/Cars/honda_civic.png" });
                AvailableCars.Add(new CarModel { ModelId = 502, Name = "Accord", Year = "2024", EngineType = "1.5T I4", BasePrice = 29000, ImagePath = "Assets/Cars/honda_accord.png" });
                AvailableCars.Add(new CarModel { ModelId = 503, Name = "CR-V", Year = "2024", EngineType = "2.0 Hybrid", BasePrice = 35000, ImagePath = "Assets/Cars/honda_crv.png" });
                AvailableCars.Add(new CarModel { ModelId = 504, Name = "Type R", Year = "2023", EngineType = "K20C1 Turbo", BasePrice = 43000, ImagePath = "Assets/Cars/honda_typr.png" });
            }
            else if (brandName == "Toyota")
            {
                AvailableCars.Add(new CarModel { ModelId = 401, Name = "Corolla", Year = "2024", EngineType = "1.6L I4", BasePrice = 21000, ImagePath = "Assets/Cars/toyota_corolla.png" });
                AvailableCars.Add(new CarModel { ModelId = 402, Name = "Camry", Year = "2024", EngineType = "2.5L I4", BasePrice = 28000, ImagePath = "Assets/Cars/toyota_camry.png" });
                AvailableCars.Add(new CarModel { ModelId = 403, Name = "RAV4", Year = "2024", EngineType = "2.5L Hybrid", BasePrice = 33000, ImagePath = "Assets/Cars/toyota_rav4.png" });
                AvailableCars.Add(new CarModel { ModelId = 404, Name = "Land Cruiser", Year = "2023", EngineType = "3.5L TT V6", BasePrice = 95000, ImagePath = "Assets/Cars/toyota_landcruiser.png" });
            }
            else if (brandName == "Audi")
            {
                AvailableCars.Add(new CarModel { ModelId = 301, Name = "A4 45 TFSI", Year = "2023", EngineType = "2.0T I4", BasePrice = 44000, ImagePath = "Assets/Cars/audi_a4.png" });
                AvailableCars.Add(new CarModel { ModelId = 302, Name = "A6 55 TFSI", Year = "2023", EngineType = "3.0T V6", BasePrice = 56000, ImagePath = "Assets/Cars/audi_a6.png" });
                AvailableCars.Add(new CarModel { ModelId = 303, Name = "Q7 55 TFSI", Year = "2023", EngineType = "3.0T V6", BasePrice = 59000, ImagePath = "Assets/Cars/audi_q7.png" });
                AvailableCars.Add(new CarModel { ModelId = 304, Name = "RS6 Avant", Year = "2022", EngineType = "4.0T V8", BasePrice = 115000, ImagePath = "Assets/Cars/audi_rs6.png" });
            }
            else if (brandName == "Mercedes-Benz")
            {
                AvailableCars.Add(new CarModel { ModelId = 201, Name = "C200", Year = "2023", EngineType = "M254 I4", BasePrice = 45000, ImagePath = "Assets/Cars/merc_c200.png" });
                AvailableCars.Add(new CarModel { ModelId = 202, Name = "E300", Year = "2023", EngineType = "M264 I4", BasePrice = 53000, ImagePath = "Assets/Cars/merc_e300.png" });
                AvailableCars.Add(new CarModel { ModelId = 203, Name = "GLC300", Year = "2024", EngineType = "M254 I4", BasePrice = 51000, ImagePath = "Assets/Cars/merc_glc.png" });
                AvailableCars.Add(new CarModel { ModelId = 204, Name = "AMG C63", Year = "2024", EngineType = "M139 Hybrid", BasePrice = 92000, ImagePath = "Assets/Cars/merc_c63.png" });
            }
            // ... add logic for other brands
        }
        private void SelectPart(object? parameter)
        {
            if (parameter is not CarPartModel part)
                return;

            // Add line to POS
            Items.Add(new PosItemViewModel
            {
                PartId = part.PartId,
                Quantity = 1,
                UnitPrice = part.UnitPrice
            });
            OnPropertyChanged(nameof(Items));

            // Back to POS screen after selecting a part
            ActiveScreen = AppScreen.Pos;
        }
        private void LoadAvailableParts(CarModel car)
        {
            AvailableParts.Clear();

            // Simple demo mapping by ModelId or Name
            // You can wire this to DB later.
            if (car.ModelId == 101) // M3 Competition
            {
                AvailableParts.Add(new CarPartModel { PartId = 10001, Code = "BMW-M3-OILFLT", Description = "Oil Filter - M3 Competition", UnitPrice = 45 });
                AvailableParts.Add(new CarPartModel { PartId = 10002, Code = "BMW-M3-AIRFLT", Description = "Air Filter - High Flow", UnitPrice = 70 });
                AvailableParts.Add(new CarPartModel { PartId = 10003, Code = "BMW-M3-BRKPAD-F", Description = "Front Brake Pads - Performance", UnitPrice = 220 });
                AvailableParts.Add(new CarPartModel { PartId = 10004, Code = "BMW-M3-SPARK", Description = "Spark Plugs Set", UnitPrice = 160 });
            }
            else if (car.ModelId == 104) // 320i
            {
                AvailableParts.Add(new CarPartModel { PartId = 11001, Code = "BMW-F30-OILFLT", Description = "Oil Filter - 320i", UnitPrice = 35 });
                AvailableParts.Add(new CarPartModel { PartId = 11002, Code = "BMW-F30-BRKPAD-F", Description = "Front Brake Pads", UnitPrice = 150 });
                AvailableParts.Add(new CarPartModel { PartId = 11003, Code = "BMW-F30-BRKPAD-R", Description = "Rear Brake Pads", UnitPrice = 130 });
            }
            // etc: add more for other ModelId

            // If none defined, you can still add some generic parts:
            if (AvailableParts.Count == 0)
            {
                AvailableParts.Add(new CarPartModel { PartId = 90001, Code = "GEN-OIL-5W40", Description = "Engine Oil 5W-40 (4L)", UnitPrice = 35 });
                AvailableParts.Add(new CarPartModel { PartId = 90002, Code = "GEN-BRKPAD-SET", Description = "Brake Pads Set (Front)", UnitPrice = 120 });
                AvailableParts.Add(new CarPartModel { PartId = 90003, Code = "GEN-AIRFLT", Description = "Air Filter - Standard", UnitPrice = 22 });
            }
        }
        private void SelectCar(object? parameter)
        {
            if (parameter is not CarModel car)
                return;

            SelectedCar = car;
            OnPropertyChanged(nameof(SelectedCar));

            // Load parts for this car
            LoadAvailableParts(car);

            // Switch to PartsSelection screen
            ActiveScreen = AppScreen.PartSelection;
        }
        private int _warehouseId = 1;
        public int WarehouseId
        {
            get => _warehouseId;
            set { _warehouseId = value; OnPropertyChanged(nameof(WarehouseId)); }
        }

        private int? _customerId;
        public int? CustomerId
        {
            get => _customerId;
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        private int _newPartId;
        public int NewPartId
        {
            get => _newPartId;
            set { _newPartId = value; OnPropertyChanged(nameof(NewPartId)); }
        }

        private int _newQuantity = 1;
        public int NewQuantity
        {
            get => _newQuantity;
            set { _newQuantity = value; OnPropertyChanged(nameof(NewQuantity)); }
        }

        private decimal _newUnitPrice;
        public decimal NewUnitPrice
        {
            get => _newUnitPrice;
            set { _newUnitPrice = value; OnPropertyChanged(nameof(NewUnitPrice)); }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount != value)
                {
                    _paidAmount = value;
                    OnPropertyChanged(nameof(PaidAmount));
                    OnPropertyChanged(nameof(RemainingAmount));
                }
            }
        }
        public decimal TotalAmount => Items.Sum(i => i.LineTotal);

        public decimal RemainingAmount => PaidAmount - TotalAmount;

        public ICommand AddLineCommand { get; }
        public ICommand SubmitSaleCommand { get; }
        public ICommand SelectBrandCommand { get; }


        public ObservableCollection<ThemeOption> Themes { get; } = new();

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

                    if (value != null)
                    {
                        ThemeManager.ApplyTheme(value.Key);
                    }
                }
            }
        }

        public PosViewModel()
        {
            Themes.Add(new ThemeOption { Key = AppTheme.Default, Name = "Default" });
            Themes.Add(new ThemeOption { Key = AppTheme.MPower, Name = "M Power" });
            Themes.Add(new ThemeOption { Key = AppTheme.NeonGlow, Name = "Neon Glow" });
            Themes.Add(new ThemeOption { Key = AppTheme.AMG, Name = "AMG" });
            Themes.Add(new ThemeOption { Key = AppTheme.PorscheRS, Name = "Porsche RS" });
            Themes.Add(new ThemeOption { Key = AppTheme.LamborghiniSC, Name = "Lamborghini Squadra Corse" });

            // Start with Default
            SelectedTheme = Themes[0];
            // Commands
            AddLineCommand = new RelayCommand(_ => AddLine());
            SubmitSaleCommand = new RelayCommand(_ => SubmitSale());
            SelectBrandCommand = new RelayCommand(SelectBrand);
            GoToPosCommand = new RelayCommand(_ => ActiveScreen = AppScreen.Pos);
            SelectCarCommand = new RelayCommand(SelectCar);
            GoToCarSelectionCommand = new RelayCommand(_ => ActiveScreen = AppScreen.CarSelection);
            SelectPartCommand = new RelayCommand(SelectPart);
            GoToHomeCommand = new RelayCommand(_ =>Reset());

            Items.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            };
            SeedBrands();
            Items = new ObservableCollection<PosItemViewModel>();
        }
        private void Reset()
        {
            ActiveScreen = AppScreen.HomePage;
            SelectedBrand = null;
        }
        private void SeedBrands()
        {
            // German brands
            GermanBrands.Add(new CarBrand { Name = "BMW", Country = "Germany", LogoPath = "Assets/Logos/bmw.png" });
            GermanBrands.Add(new CarBrand { Name = "Mercedes-Benz", Country = "Germany", LogoPath = "Assets/Logos/mercedes.png" });
            GermanBrands.Add(new CarBrand { Name = "Audi", Country = "Germany", LogoPath = "Assets/Logos/audi.png" });
            GermanBrands.Add(new CarBrand { Name = "Volkswagen", Country = "Germany", LogoPath = "Assets/Logos/volkswagen.png" });
            GermanBrands.Add(new CarBrand { Name = "Porsche", Country = "Germany", LogoPath = "Assets/Logos/porsche.png" });
            GermanBrands.Add(new CarBrand { Name = "Opel", Country = "Germany", LogoPath = "Assets/Logos/opel.png" });

            // Japanese brands
            JapaneseBrands.Add(new CarBrand { Name = "Toyota", Country = "Japan", LogoPath = "Assets/Logos/toyota.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Honda", Country = "Japan", LogoPath = "Assets/Logos/honda.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Nissan", Country = "Japan", LogoPath = "Assets/Logos/nissan.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Mazda", Country = "Japan", LogoPath = "Assets/Logos/mazda.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Subaru", Country = "Japan", LogoPath = "Assets/Logos/subaru.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Mitsubishi", Country = "Japan", LogoPath = "Assets/Logos/mitsubishi.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Suzuki", Country = "Japan", LogoPath = "Assets/Logos/suzuki.png" });
            JapaneseBrands.Add(new CarBrand { Name = "Lexus", Country = "Japan", LogoPath = "Assets/Logos/lexus.png" });

            //KoreanBrands
            KoreanBrands.Add(new CarBrand { Name = "Kia", Country = "Korea", LogoPath = "Assets/Logos/Kia.png" });
            KoreanBrands.Add(new CarBrand { Name = "Hyundai", Country = "Korea", LogoPath = "Assets/Logos/Hyundai.png" });
        }


        private void BrandExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var expanded = sender as Expander;
            if (expanded == null) return;

            // Get the StackPanel that contains all the expanders
            var parent = VisualTreeHelper.GetParent(expanded) as Panel;
            if (parent == null) return;

            foreach (var child in parent.Children.OfType<Expander>())
            {
                if (!ReferenceEquals(child, expanded))
                    child.IsExpanded = false;
            }
        }
        private void AddLine()
        {
            if (NewPartId <= 0 || NewQuantity <= 0 || NewUnitPrice <= 0)
            {
                CustomMessageBox.Show("Please fill Part ID, Quantity and Unit Price with positive values before adding a line.", "Validation Error", "Warning");
                return;
            }

            Items.Add(new PosItemViewModel
            {
                PartId = NewPartId,
                Quantity = NewQuantity,
                UnitPrice = NewUnitPrice
            });

            NewPartId = 0;
            NewQuantity = 1;
            NewUnitPrice = 0;
            OnPropertyChanged(nameof(NewPartId));
            OnPropertyChanged(nameof(NewQuantity));
            OnPropertyChanged(nameof(NewUnitPrice));
            OnPropertyChanged(nameof(Items));
            Items.Add(new PosItemViewModel
            {
                PartId = NewPartId,
                Quantity = NewQuantity,
                UnitPrice = NewUnitPrice
            });

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(RemainingAmount));
        }

        private void SubmitSale()
        {
            if (Items.Count == 0)
            {
                CustomMessageBox.Show("Add at least one line before submitting the sale.", "Validation Error", "Warning");

                return;
            }

            try
            {
                var client = new HttpClient();
                client.BaseAddress = new Uri("http://localhost:5000/"); // adjust to your API port

                var req = new CreateSaleRequest
                {
                    InvoiceDate = DateTime.Now,
                    CustomerId = this.CustomerId,
                    WarehouseId = this.WarehouseId,
                    PaymentMethod = "Cash",
                    PaidAmount = this.PaidAmount,
                    Notes = SelectedBrand != null ? $"Sale for brand {SelectedBrand.Name}" : "WPF POS Sale"
                };

                foreach (var item in Items)
                {
                    req.Items.Add(new SaleItemDto
                    {
                        PartId = item.PartId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountAmount = 0,
                        TaxRate = 0
                    });
                }

                var json = JsonSerializer.Serialize(req);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = client.PostAsync("api/sales", content).Result;
                if (!response.IsSuccessStatusCode)
                {
                    CustomMessageBox.Show($"Error from API: {response.StatusCode}. Make sure the API is running.", "Validation Error", "Warning");
                    return;
                }

                var respJson = response.Content.ReadAsStringAsync().Result;
                var result = JsonSerializer.Deserialize<CreateSaleResponse>(respJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                CustomMessageBox.Show($"Sale created. Invoice: {result?.InvoiceNumber}, Total: {result?.TotalAmount}", "Validation Error", "Warning");

                Items.Clear();
                PaidAmount = 0;
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(PaidAmount));
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error while submitting sale: {ex.Message}", "Validation Error", "Warning");

            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }

}
