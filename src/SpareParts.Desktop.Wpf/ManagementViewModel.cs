using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

// All DTO types used here come from SpareParts.Domain:
//   CustomerDto, CreateCustomerRequest  → SpareParts.Domain.BusinessPartners
//   SupplierDto, CreateSupplierRequest  → SpareParts.Domain.BusinessPartners
//   BrandDto, CreateBrandRequest        → SpareParts.Domain.Inventory
//   PartDto, CreatePartRequest          → SpareParts.Domain.Inventory
//   CarBrandDto, CarModelDto            → SpareParts.Domain.Cars
//   CreateCarBrandRequest               → SpareParts.Domain.Cars
//   CreateCarModelRequest               → SpareParts.Domain.Cars

namespace SpareParts.Desktop.Wpf
{
    public class ManagementViewModel : INotifyPropertyChanged
    {
        private static readonly string Base = "http://localhost:5000/";
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri(Base) };
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        // ── Lists ─────────────────────────────────────────────────────────────
        public UsersViewModel                      UsersVm    { get; } = new();
        public ObservableCollection<CustomerDto>   Customers  { get; } = new();
        public ObservableCollection<SupplierDto>   Suppliers  { get; } = new();
        public ObservableCollection<BrandDto>      Brands     { get; } = new();
        public ObservableCollection<PartDto>       Parts      { get; } = new();
        public ObservableCollection<CarModelDto>   CarModels  { get; } = new();

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
        public ICommand LoadAllCommand    { get; }
        public ICommand SaveCustomerCommand  { get; }
        public ICommand SaveSupplierCommand  { get; }
        public ICommand SaveBrandCommand     { get; }
        public ICommand SavePartCommand      { get; }
        public ICommand SaveCarModelCommand  { get; }

        public ManagementViewModel()
        {
            LoadAllCommand       = new RelayCommand(_ => LoadAll());
            SaveCustomerCommand  = new RelayCommand(_ => SaveCustomer());
            SaveSupplierCommand  = new RelayCommand(_ => SaveSupplier());
            SaveBrandCommand     = new RelayCommand(_ => SaveBrand());
            SavePartCommand      = new RelayCommand(_ => SavePart());
            SaveCarModelCommand  = new RelayCommand(_ => SaveCarModel());
        }

        // ── Load all ─────────────────────────────────────────────────────────
        public async void LoadAll()
        {
            try
            {
                var customers = await _http.GetFromJsonAsync<System.Collections.Generic.List<CustomerDto>>("api/customers", _json) ?? new();
                var suppliers = await _http.GetFromJsonAsync<System.Collections.Generic.List<SupplierDto>>("api/suppliers", _json) ?? new();
                var brands    = await _http.GetFromJsonAsync<System.Collections.Generic.List<BrandDto>>("api/brands", _json) ?? new();
                var parts     = await _http.GetFromJsonAsync<System.Collections.Generic.List<PartDto>>("api/parts", _json) ?? new();
                var carModels = await _http.GetFromJsonAsync<System.Collections.Generic.List<CarModelDto>>("api/carmodels", _json) ?? new();

                Customers.Clear();  foreach (var x in customers)  Customers.Add(x);
                Suppliers.Clear();  foreach (var x in suppliers)  Suppliers.Add(x);
                Brands.Clear();     foreach (var x in brands)     Brands.Add(x);
                Parts.Clear();      foreach (var x in parts)      Parts.Add(x);
                CarModels.Clear();  foreach (var x in carModels)  CarModels.Add(x);

                Status = "✓ Data loaded.";
            }
            catch (Exception ex) { Status = $"✗ Load failed: {ex.Message}"; }
        }

        // ── Save helpers ──────────────────────────────────────────────────────
        private async void Post(string url, object payload, string entityName)
        {
            try
            {
                var json     = JsonSerializer.Serialize(payload);
                var content  = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _http.PostAsync(url, content);

                Status = response.IsSuccessStatusCode
                    ? $"✓ {entityName} saved."
                    : $"✗ Error saving {entityName}: {response.StatusCode}";

                if (response.IsSuccessStatusCode) LoadAll();
            }
            catch (Exception ex) { Status = $"✗ {ex.Message}"; }
        }

        private void SaveCustomer()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName)) { Status = "✗ Customer name is required."; return; }
            Post("api/customers", new CreateCustomerRequest
            {
                Name = NewCustomerName, Phone = NewCustomerPhone, Email = NewCustomerEmail,
                Address = NewCustomerAddress, TaxNumber = NewCustomerTax, OpeningBalance = NewCustomerBalance
            }, "Customer");
            NewCustomerName = NewCustomerPhone = NewCustomerEmail = NewCustomerAddress = NewCustomerTax = string.Empty;
            NewCustomerBalance = 0;
            OnPropertyChanged(nameof(NewCustomerName)); OnPropertyChanged(nameof(NewCustomerPhone));
            OnPropertyChanged(nameof(NewCustomerEmail)); OnPropertyChanged(nameof(NewCustomerAddress));
            OnPropertyChanged(nameof(NewCustomerTax));  OnPropertyChanged(nameof(NewCustomerBalance));
        }

        private void SaveSupplier()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName)) { Status = "✗ Supplier name is required."; return; }
            Post("api/suppliers", new CreateSupplierRequest
            {
                Name = NewSupplierName, Phone = NewSupplierPhone, Email = NewSupplierEmail,
                Address = NewSupplierAddress, TaxNumber = NewSupplierTax, OpeningBalance = NewSupplierBalance
            }, "Supplier");
            NewSupplierName = NewSupplierPhone = NewSupplierEmail = NewSupplierAddress = NewSupplierTax = string.Empty;
            NewSupplierBalance = 0;
            OnPropertyChanged(nameof(NewSupplierName)); OnPropertyChanged(nameof(NewSupplierPhone));
            OnPropertyChanged(nameof(NewSupplierEmail)); OnPropertyChanged(nameof(NewSupplierAddress));
            OnPropertyChanged(nameof(NewSupplierTax));  OnPropertyChanged(nameof(NewSupplierBalance));
        }

        private void SaveBrand()
        {
            if (string.IsNullOrWhiteSpace(NewBrandName)) { Status = "✗ Brand name is required."; return; }
            Post("api/brands", new CreateBrandRequest { Name = NewBrandName, IsActive = NewBrandIsActive }, "Brand");
            NewBrandName = string.Empty;
            OnPropertyChanged(nameof(NewBrandName));
        }

        private void SavePart()
        {
            if (string.IsNullOrWhiteSpace(NewPartCode) || string.IsNullOrWhiteSpace(NewPartName))
            { Status = "✗ Part code and name are required."; return; }

            Post("api/parts", new CreatePartRequest
            {
                InternalCode = NewPartCode,
                Name         = NewPartName,
                OEMNumber    = NewPartOEM,
                Condition    = SpareParts.Domain.Inventory.PartCondition.New,
                CategoryId   = NewPartCategoryId,
                BrandId      = NewPartBrandId,
                CostPrice    = NewPartCostPrice,
                SalePrice    = NewPartSalePrice,
                Currency     = NewPartCurrency,
                MinStock     = NewPartMinStock,
                Notes        = NewPartNotes
            }, "Part");

            NewPartCode = NewPartName = NewPartOEM = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            OnPropertyChanged(nameof(NewPartCode)); OnPropertyChanged(nameof(NewPartName));
            OnPropertyChanged(nameof(NewPartOEM));  OnPropertyChanged(nameof(NewPartNotes));
        }

        private void SaveCarModel()
        {
            if (string.IsNullOrWhiteSpace(NewCarModelName)) { Status = "✗ Car model name is required."; return; }
            Post("api/carmodels", new CreateCarModelRequest
            {
                Name       = NewCarModelName,
                Year       = NewCarModelYear,
                EngineType = NewCarModelEngine,
                BasePrice  = NewCarModelBasePrice,
                CarBrandId = NewCarModelBrandId
            }, "Car Model");
            NewCarModelName = NewCarModelYear = NewCarModelEngine = string.Empty;
            NewCarModelBasePrice = 0;
            OnPropertyChanged(nameof(NewCarModelName)); OnPropertyChanged(nameof(NewCarModelYear));
            OnPropertyChanged(nameof(NewCarModelEngine));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
