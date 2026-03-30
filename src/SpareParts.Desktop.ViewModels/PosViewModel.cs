using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

// CarBrand / CarModel local UI-only models kept here because they are purely
// presentational (hold LogoPath, AvailableCars, etc.) and are NOT persisted.
// All API-crossing DTOs come from SpareParts.Domain.

namespace SpareParts.Desktop.Wpf
{
    // ── UI-only display models (never sent to API) ────────────────────────────
    public class PosViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<PosItemViewModel> Items { get; } = new();

        // Brand lists
        public ObservableCollection<CarBrandUi> GermanBrands   { get; } = new();
        public ObservableCollection<CarBrandUi> JapaneseBrands { get; } = new();
        public ObservableCollection<CarBrandUi> KoreanBrands   { get; } = new();

        private CarBrandUi? _selectedBrand;
        public CarBrandUi? SelectedBrand
        {
            get => _selectedBrand;
            set { _selectedBrand = value; OnPropertyChanged(nameof(SelectedBrand)); }
        }

        private CarModelUi? _selectedCar;
        public CarModelUi? SelectedCar
        {
            get => _selectedCar;
            set { _selectedCar = value; OnPropertyChanged(nameof(SelectedCar)); }
        }

        public ObservableCollection<CarModelUi>  AvailableCars  { get; } = new();
        public ObservableCollection<CarPartModel> AvailableParts { get; } = new();

        private AppScreen _activeScreen = AppScreen.HomePage;
        public AppScreen ActiveScreen
        {
            get => _activeScreen;
            set { if (_activeScreen != value) { _activeScreen = value; OnPropertyChanged(nameof(ActiveScreen)); } }
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

        private DateTime _invoiceDate = DateTime.Today;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                _invoiceDate = value;
                OnPropertyChanged(nameof(InvoiceDate));
            }
        }

        public decimal TotalAmount    => Items.Sum(i => i.LineTotal);
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        // ── New-line entry ────────────────────────────────────────────────────
        public int     NewPartId    { get; set; }
        public int     NewQuantity  { get; set; } = 1;
        public decimal NewUnitPrice { get; set; }

        // ── Dependencies / Commands ──────────────────────────────────────────
        private readonly ISalesApiClient _salesApi;
        private readonly ICarCatalogApiClient _carCatalogApi;
        private readonly IPartsApiClient _partsApi;

        public ICommand SelectBrandCommand  { get; }
        public ICommand SelectCarCommand    { get; }
        public ICommand SelectPartCommand   { get; }
        public ICommand AddItemCommand      { get; }
        public ICommand SubmitSaleCommand   { get; }
        public ICommand GoHomeCommand       { get; }

        public PosViewModel(
            ISalesApiClient salesApi,
            ICarCatalogApiClient carCatalogApi,
            IPartsApiClient partsApi)
        {
            _salesApi = salesApi;
            _carCatalogApi = carCatalogApi;
            _partsApi = partsApi;

            SelectBrandCommand = new RelayCommand(SelectBrand);
            SelectCarCommand   = new RelayCommand(SelectCar);
            SelectPartCommand  = new RelayCommand(SelectPart);
            AddItemCommand     = new RelayCommand(_ => AddItem());
            SubmitSaleCommand  = new RelayCommand(_ => SubmitSaleAsync());
            GoHomeCommand      = new RelayCommand(_ => ActiveScreen = AppScreen.HomePage);

            SeedBrands();
        }

        private void AddItem()
        {
            if (NewPartId <= 0 || NewQuantity <= 0 || NewUnitPrice <= 0)
            {
                CustomMessageBox.Show("Please fill in Part ID, Quantity, and Unit Price.", "Validation Error", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Please fill in Part ID, Quantity, and Unit Price.", false);
                return;
            }

            Items.Add(new PosItemViewModel
            {
                PartId    = NewPartId,
                Quantity  = NewQuantity,
                UnitPrice = NewUnitPrice
            });

            NewPartId = 0; NewQuantity = 1; NewUnitPrice = 0;
            OnPropertyChanged(nameof(NewPartId));
            OnPropertyChanged(nameof(NewQuantity));
            OnPropertyChanged(nameof(NewUnitPrice));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(RemainingAmount));
        }

        private async void SubmitSaleAsync()
        {
            if (Items.Count == 0)
            {
                CustomMessageBox.Show("Add at least one line before submitting the sale.", "Validation Error", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Add at least one line before submitting the sale.", false);
                return;
            }

            try
            {
                // Use the sales API client (authorization token is read from shared token store)
                var req = new CreateSaleRequest
                {
                    InvoiceDate   = InvoiceDate,
                    CustomerId    = CustomerId,
                    WarehouseId   = WarehouseId,
                    PaymentMethod = "Cash",
                    PaidAmount    = PaidAmount,
                    Notes         = SelectedBrand != null
                                        ? $"Sale for brand {SelectedBrand.Name}"
                                        : "WPF POS Sale",
                    Items = Items.Select(i => new SaleItemDto
                    {
                        PartId         = i.PartId,
                        Quantity       = i.Quantity,
                        UnitPrice      = i.UnitPrice,
                        DiscountAmount = 0,
                        TaxRate        = 0
                    }).ToList()
                };

                var result = await _salesApi.CreateSaleAsync(req);

                CustomMessageBox.Show(
                    $"Sale created. Invoice: {result?.InvoiceNumber}, Total: {result?.TotalAmount:N2}",
                    "Success", "Info");
                AppNotificationCenter.Instance.Publish($"✓ Sale created. Invoice: {result?.InvoiceNumber}", true);

                Items.Clear();
                PaidAmount = 0;
                OnPropertyChanged(nameof(Items));
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(PaidAmount));
            }
            catch (ApiClientException ex)
            {
                CustomMessageBox.Show($"Error while submitting sale: {ex.Message}", "Error", "Warning");
                AppNotificationCenter.Instance.Publish($"✗ API error ({ex.Code}): {ex.Message}", false);
            }
            catch (Exception)
            {
                CustomMessageBox.Show("Unexpected error while submitting sale.", "Error", "Warning");
                AppNotificationCenter.Instance.Publish("✗ Unexpected error while submitting sale.", false);
            }
        }

        private async void SelectBrand(object? parameter)
        {
            if (parameter is not CarBrandUi brand) return;
            SelectedBrand = brand;
            OnPropertyChanged(nameof(SelectedBrand));
            await LoadAvailableCarsAsync(brand.Name);
            ActiveScreen = AppScreen.CarSelection;
        }

        private async void SelectCar(object? parameter)
        {
            if (parameter is not CarModelUi car) return;
            SelectedCar = car;
            await LoadAvailablePartsAsync(car);
            ActiveScreen = AppScreen.PartSelection;
        }

        private void SelectPart(object? parameter)
        {
            if (parameter is not CarPartModel part) return;
            Items.Add(new PosItemViewModel
            {
                PartId      = part.PartId,
                Description = part.Description,
                Quantity    = 1,
                UnitPrice   = part.UnitPrice
            });
            OnPropertyChanged(nameof(Items));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(RemainingAmount));
            ActiveScreen = AppScreen.Pos;
        }

        private async Task LoadAvailableCarsAsync(string? brandName)
        {
            AvailableCars.Clear();
            if (string.IsNullOrWhiteSpace(brandName))
                return;

            var brands = await _carCatalogApi.GetCarBrandsAsync();
            var brand = brands.FirstOrDefault(
                b => string.Equals(b.Name, brandName, StringComparison.OrdinalIgnoreCase));
            if (brand == null) return;

            var models = await _carCatalogApi.GetCarModelsAsync(brand.Id);
            foreach (var model in models)
            {
                AvailableCars.Add(new CarModelUi
                {
                    Id = model.Id,
                    Name = model.Name,
                    Year = model.Year
                });
            }
        }

        private async Task LoadAvailablePartsAsync(CarModelUi car)
        {
            AvailableParts.Clear();
            var parts = await _partsApi.GetPartsAsync();
            foreach (var part in parts)
            {
                AvailableParts.Add(new CarPartModel
                {
                    PartId = part.Id,
                    Description = part.Name,
                    UnitPrice = part.SalePrice
                });
            }
        }

        private void SeedBrands()
        {
            void Add(ObservableCollection<CarBrandUi> col, string name, string country, string logo)
                => col.Add(new CarBrandUi { Name = name, Country = country, LogoPath = logo });

            Add(GermanBrands,   "BMW",           "Germany", "Assets/Logos/bmw.png");
            Add(GermanBrands,   "Mercedes-Benz", "Germany", "Assets/Logos/mercedes.png");
            Add(GermanBrands,   "Audi",          "Germany", "Assets/Logos/audi.png");
            Add(GermanBrands,   "Volkswagen",    "Germany", "Assets/Logos/volkswagen.png");
            Add(GermanBrands,   "Porsche",       "Germany", "Assets/Logos/porsche.png");
            Add(GermanBrands,   "Opel",          "Germany", "Assets/Logos/opel.png");

            Add(JapaneseBrands, "Toyota",        "Japan",   "Assets/Logos/toyota.png");
            Add(JapaneseBrands, "Honda",         "Japan",   "Assets/Logos/honda.png");
            Add(JapaneseBrands, "Nissan",        "Japan",   "Assets/Logos/nissan.png");
            Add(JapaneseBrands, "Mazda",         "Japan",   "Assets/Logos/mazda.png");
            Add(JapaneseBrands, "Subaru",        "Japan",   "Assets/Logos/subaru.png");
            Add(JapaneseBrands, "Mitsubishi",    "Japan",   "Assets/Logos/mitsubishi.png");
            Add(JapaneseBrands, "Suzuki",        "Japan",   "Assets/Logos/suzuki.png");
            Add(JapaneseBrands, "Lexus",         "Japan",   "Assets/Logos/lexus.png");

            Add(KoreanBrands,   "Kia",           "Korea",   "Assets/Logos/kia.png");
            Add(KoreanBrands,   "Hyundai",       "Korea",   "Assets/Logos/hyundai.png");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
