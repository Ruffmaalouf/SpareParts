using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public class ManagementViewModel : INotifyPropertyChanged
    {
   
        private static IApiClient Api => ApiClient.Instance; 

        // ── Lists ─────────────────────────────────────────────────────────────
        public UsersViewModel                      UsersVm    { get; } = new();
        public RolesViewModel                      RolesVm    { get; } = new();
        public ObservableCollection<CustomerDto>   Customers  { get; } = new();
        public ObservableCollection<SupplierDto>   Suppliers  { get; } = new();
        public ObservableCollection<BrandDto>      Brands     { get; } = new();
        public ObservableCollection<CarBrandDto>   CarBrands  { get; } = new();
        public ObservableCollection<CategoryDto>   Categories { get; } = new();
        public ObservableCollection<PartDto>       Parts      { get; } = new();
        public ObservableCollection<CarModelDto>   CarModels  { get; } = new();

        private CustomerDto? _selectedCustomer;
        public CustomerDto? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged(nameof(SelectedCustomer));
                if (value != null) PopulateCustomerForm(value);
            }
        }

        private SupplierDto? _selectedSupplier;
        public SupplierDto? SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                _selectedSupplier = value;
                OnPropertyChanged(nameof(SelectedSupplier));
                if (value != null) PopulateSupplierForm(value);
            }
        }

        private BrandDto? _selectedBrand;
        public BrandDto? SelectedBrand
        {
            get => _selectedBrand;
            set
            {
                _selectedBrand = value;
                OnPropertyChanged(nameof(SelectedBrand));
                if (value != null) PopulateBrandForm(value);
            }
        }

        private PartDto? _selectedPart;
        public PartDto? SelectedPart
        {
            get => _selectedPart;
            set
            {
                _selectedPart = value;
                OnPropertyChanged(nameof(SelectedPart));
                if (value != null) PopulatePartForm(value);
            }
        }

        private CarModelDto? _selectedCarModel;
        public CarModelDto? SelectedCarModel
        {
            get => _selectedCarModel;
            set
            {
                _selectedCarModel = value;
                OnPropertyChanged(nameof(SelectedCarModel));
                if (value != null) PopulateCarModelForm(value);
            }
        }

        // ── Status ────────────────────────────────────────────────────────────
        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        // ── New Customer ──────────────────────────────────────────────────────
        public string  NewCustomerName     { get; set; } = string.Empty;
        public string  NewCustomerPhone    { get; set; } = string.Empty;
        public string  NewCustomerEmail    { get; set; } = string.Empty;
        public string  NewCustomerAddress  { get; set; } = string.Empty;
        public string  NewCustomerTax      { get; set; } = string.Empty;
        public decimal NewCustomerBalance  { get; set; }

        // ── New Supplier ──────────────────────────────────────────────────────
        public string  NewSupplierName     { get; set; } = string.Empty;
        public string  NewSupplierPhone    { get; set; } = string.Empty;
        public string  NewSupplierEmail    { get; set; } = string.Empty;
        public string  NewSupplierAddress  { get; set; } = string.Empty;
        public string  NewSupplierTax      { get; set; } = string.Empty;
        public decimal NewSupplierBalance  { get; set; }

        // ── New Brand ─────────────────────────────────────────────────────────
        public string NewBrandName     { get; set; } = string.Empty;
        public bool   NewBrandIsActive { get; set; } = true;

        // ── New Part ──────────────────────────────────────────────────────────
        public string  NewPartCode       { get; set; } = string.Empty;
        public string  NewPartName       { get; set; } = string.Empty;
        public string  NewPartOEM        { get; set; } = string.Empty;
        public decimal NewPartCostPrice  { get; set; }
        public decimal NewPartSalePrice  { get; set; }
        public string  NewPartCurrency   { get; set; } = "USD";
        public int     NewPartMinStock   { get; set; }
        public int     NewPartCategoryId { get; set; } = 1;
        public int?    NewPartBrandId    { get; set; }
        public string  NewPartNotes      { get; set; } = string.Empty;

        // ── New Car Model ─────────────────────────────────────────────────────
        public string  NewCarModelName      { get; set; } = string.Empty;
        public string  NewCarModelYear      { get; set; } = string.Empty;
        public string  NewCarModelEngine    { get; set; } = string.Empty;
        public decimal NewCarModelBasePrice { get; set; }
        public int     NewCarModelBrandId   { get; set; }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand LoadAllCommand       { get; }
        public ICommand SaveCustomerCommand  { get; }
        public ICommand SaveSupplierCommand  { get; }
        public ICommand SaveBrandCommand     { get; }
        public ICommand SavePartCommand      { get; }
        public ICommand SaveCarModelCommand  { get; }

        public ManagementViewModel()
        {
            LoadAllCommand      = new RelayCommand(_ => _ = LoadAllAsync());
            SaveCustomerCommand = new RelayCommand(_ => _ = SaveCustomerAsync());
            SaveSupplierCommand = new RelayCommand(_ => _ = SaveSupplierAsync());
            SaveBrandCommand    = new RelayCommand(_ => _ = SaveBrandAsync());
            SavePartCommand     = new RelayCommand(_ => _ = SavePartAsync());
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
        }

        // ── Load all ──────────────────────────────────────────────────────────
        public async Task LoadAllAsync()
        {
            Status = "Loading…";
            try
            {
                var customers = await Api.GetAllAsync<CustomerDto>("api/customers");
                var suppliers = await Api.GetAllAsync<SupplierDto>("api/suppliers");
                var brands    = await Api.GetAllAsync<BrandDto>("api/brands");
                var carBrands = await Api.GetCarBrandsAsync();
                var categories = await Api.GetAllAsync<CategoryDto>("api/categories");
                var parts     = await Api.GetAllAsync<PartDto>("api/parts");
                var carModels = await Api.GetAllAsync<CarModelDto>("api/carmodels");
                await RolesVm.LoadAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Customers.Clear(); foreach (var x in customers)  Customers.Add(x);
                    Suppliers.Clear(); foreach (var x in suppliers)  Suppliers.Add(x);
                    Brands.Clear();    foreach (var x in brands)     Brands.Add(x);
                    CarBrands.Clear(); foreach (var x in carBrands)  CarBrands.Add(x);
                    Categories.Clear(); foreach (var x in categories) Categories.Add(x);
                    Parts.Clear();     foreach (var x in parts)      Parts.Add(x);
                    CarModels.Clear(); foreach (var x in carModels)  CarModels.Add(x);
                });

                Status = "✓ Data loaded.";
            }
            catch (Exception ex) { Status = $"✗ Load failed: {ex.Message}"; }
        }

        // ── Save: Customer ────────────────────────────────────────────────────
        private async Task SaveCustomerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName)) { Status = "✗ Customer name is required."; return; }
            await PostAsync("api/customers", new CreateCustomerRequest
            {
                Name = NewCustomerName, Phone = NewCustomerPhone, Email = NewCustomerEmail,
                Address = NewCustomerAddress, TaxNumber = NewCustomerTax, OpeningBalance = NewCustomerBalance
            }, "Customer");
            NewCustomerName = NewCustomerPhone = NewCustomerEmail =
            NewCustomerAddress = NewCustomerTax = string.Empty;
            NewCustomerBalance = 0;
            RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail),
                     nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        }

        // ── Save: Supplier ────────────────────────────────────────────────────
        private async Task SaveSupplierAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName)) { Status = "✗ Supplier name is required."; return; }
            await PostAsync("api/suppliers", new CreateSupplierRequest
            {
                Name = NewSupplierName, Phone = NewSupplierPhone, Email = NewSupplierEmail,
                Address = NewSupplierAddress, TaxNumber = NewSupplierTax, OpeningBalance = NewSupplierBalance
            }, "Supplier");
            NewSupplierName = NewSupplierPhone = NewSupplierEmail =
            NewSupplierAddress = NewSupplierTax = string.Empty;
            NewSupplierBalance = 0;
            RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail),
                     nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        }

        // ── Save: Brand ───────────────────────────────────────────────────────
        private async Task SaveBrandAsync()
        {
            if (string.IsNullOrWhiteSpace(NewBrandName)) { Status = "✗ Brand name is required."; return; }
            await PostAsync("api/brands", new CreateBrandRequest { Name = NewBrandName, IsActive = NewBrandIsActive }, "Brand");
            NewBrandName = string.Empty;
            RaiseAll(nameof(NewBrandName));
        }

        // ── Save: Part ────────────────────────────────────────────────────────
        private async Task SavePartAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPartCode) || string.IsNullOrWhiteSpace(NewPartName))
            { Status = "✗ Part code and name are required."; return; }
            await PostAsync("api/parts", new CreatePartRequest
            {
                InternalCode = NewPartCode, Name = NewPartName, OEMNumber = NewPartOEM,
                Condition    = PartCondition.New, CategoryId = NewPartCategoryId, BrandId = NewPartBrandId,
                CostPrice    = NewPartCostPrice,  SalePrice  = NewPartSalePrice,
                Currency     = NewPartCurrency,   MinStock   = NewPartMinStock, Notes = NewPartNotes
            }, "Part");
            NewPartCode = NewPartName = NewPartOEM = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM),
                     nameof(NewPartNotes), nameof(NewPartCostPrice), nameof(NewPartSalePrice));
        }

        // ── Save: Car Model ───────────────────────────────────────────────────
        private async Task SaveCarModelAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCarModelName)) { Status = "✗ Car model name is required."; return; }
            await PostAsync("api/carmodels", new CreateCarModelRequest
            {
                Name = NewCarModelName, Year = NewCarModelYear, EngineType = NewCarModelEngine,
                BasePrice = NewCarModelBasePrice, CarBrandId = NewCarModelBrandId
            }, "Car Model");
            NewCarModelName = NewCarModelYear = NewCarModelEngine = string.Empty;
            NewCarModelBasePrice = 0;
            RaiseAll(nameof(NewCarModelName), nameof(NewCarModelYear),
                     nameof(NewCarModelEngine), nameof(NewCarModelBasePrice));
        }

        // ── Shared POST via ApiClient (has token) ─────────────────────────────
        private async Task PostAsync(string url, object payload, string entityName)
        {
            try
            {
                await Api.PostAsync(url, payload);
                Status = $"✓ {entityName} saved.";
                await LoadAllAsync();
            }
            catch (Exception ex) { Status = $"✗ Error saving {entityName}: {ex.Message}"; }
        }

        private void RaiseAll(params string[] names)
        {
            foreach (var n in names) OnPropertyChanged(n);
        }

        private void PopulateCustomerForm(CustomerDto c)
        {
            NewCustomerName = c.Name;
            NewCustomerPhone = c.Phone;
            NewCustomerEmail = c.Email;
            NewCustomerAddress = c.Address;
            NewCustomerTax = c.TaxNumber;
            NewCustomerBalance = c.OpeningBalance;
            RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail),
                     nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        }

        private void PopulateSupplierForm(SupplierDto s)
        {
            NewSupplierName = s.Name;
            NewSupplierPhone = s.Phone;
            NewSupplierEmail = s.Email;
            NewSupplierAddress = s.Address;
            NewSupplierTax = s.TaxNumber;
            NewSupplierBalance = s.OpeningBalance;
            RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail),
                     nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        }

        private void PopulateBrandForm(BrandDto b)
        {
            NewBrandName = b.Name;
            NewBrandIsActive = b.IsActive;
            RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive));
        }

        private void PopulatePartForm(PartDto p)
        {
            NewPartCode = p.InternalCode;
            NewPartName = p.Name;
            NewPartOEM = p.OEMNumber;
            NewPartCategoryId = p.CategoryId;
            NewPartBrandId = p.BrandId;
            NewPartCostPrice = p.CostPrice;
            NewPartSalePrice = p.SalePrice;
            NewPartCurrency = p.Currency;
            NewPartMinStock = p.MinStock;
            NewPartNotes = p.Notes;
            RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM),
                     nameof(NewPartCategoryId), nameof(NewPartBrandId), nameof(NewPartCostPrice),
                     nameof(NewPartSalePrice), nameof(NewPartCurrency), nameof(NewPartMinStock),
                     nameof(NewPartNotes));
        }

        private void PopulateCarModelForm(CarModelDto m)
        {
            NewCarModelBrandId = m.CarBrandId;
            NewCarModelName = m.Name;
            NewCarModelYear = m.Year;
            NewCarModelEngine = m.EngineType;
            NewCarModelBasePrice = m.BasePrice;
            RaiseAll(nameof(NewCarModelBrandId), nameof(NewCarModelName), nameof(NewCarModelYear),
                     nameof(NewCarModelEngine), nameof(NewCarModelBasePrice));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
