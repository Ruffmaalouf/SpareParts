using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    // ── Lightweight display models (returned from API GET lists) ─────────────

    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class SupplierDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class BrandDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class PartDto
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public bool IsActive { get; set; }
    }

    public class CarModelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string EngineType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ManagementViewModel
    // ══════════════════════════════════════════════════════════════════════════
    public class ManagementViewModel : INotifyPropertyChanged
    {
        private static readonly string Base = "http://localhost:5000/";
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri(Base) };
        private readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

        // ── Lists ─────────────────────────────────────────────────────────────
        public ObservableCollection<CustomerDto>  Customers  { get; } = new();
        public ObservableCollection<SupplierDto>  Suppliers  { get; } = new();
        public ObservableCollection<BrandDto>     Brands     { get; } = new();
        public ObservableCollection<PartDto>      Parts      { get; } = new();
        public ObservableCollection<CarModelDto>  CarModels  { get; } = new();

        // ── Status message ────────────────────────────────────────────────────
        private string _status = string.Empty;
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        // ══════════════════════════════════════════
        // NEW CUSTOMER fields
        // ══════════════════════════════════════════
        public string NewCustomerName     { get; set; } = string.Empty;
        public string NewCustomerPhone    { get; set; } = string.Empty;
        public string NewCustomerEmail    { get; set; } = string.Empty;
        public string NewCustomerAddress  { get; set; } = string.Empty;
        public string NewCustomerTax      { get; set; } = string.Empty;
        public decimal NewCustomerBalance { get; set; }

        // ══════════════════════════════════════════
        // NEW SUPPLIER fields
        // ══════════════════════════════════════════
        public string NewSupplierName     { get; set; } = string.Empty;
        public string NewSupplierPhone    { get; set; } = string.Empty;
        public string NewSupplierEmail    { get; set; } = string.Empty;
        public string NewSupplierAddress  { get; set; } = string.Empty;
        public string NewSupplierTax      { get; set; } = string.Empty;
        public decimal NewSupplierBalance { get; set; }

        // ══════════════════════════════════════════
        // NEW BRAND fields
        // ══════════════════════════════════════════
        public string NewBrandName       { get; set; } = string.Empty;
        public bool   NewBrandIsActive   { get; set; } = true;

        // ══════════════════════════════════════════
        // NEW PART fields
        // ══════════════════════════════════════════
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

        // ══════════════════════════════════════════
        // NEW CAR MODEL fields
        // ══════════════════════════════════════════
        public string  NewCarModelName       { get; set; } = string.Empty;
        public string  NewCarModelYear       { get; set; } = string.Empty;
        public string  NewCarModelEngine     { get; set; } = string.Empty;
        public decimal NewCarModelBasePrice  { get; set; }
        public int?    NewCarModelBrandId    { get; set; }

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand LoadAllCommand       { get; }
        public ICommand SaveCustomerCommand  { get; }
        public ICommand SaveSupplierCommand  { get; }
        public ICommand SaveBrandCommand     { get; }
        public ICommand SavePartCommand      { get; }
        public ICommand SaveCarModelCommand  { get; }

        public ManagementViewModel()
        {
            LoadAllCommand      = new RelayCommand(_ => LoadAll());
            SaveCustomerCommand = new RelayCommand(_ => SaveCustomer());
            SaveSupplierCommand = new RelayCommand(_ => SaveSupplier());
            SaveBrandCommand    = new RelayCommand(_ => SaveBrand());
            SavePartCommand     = new RelayCommand(_ => SavePart());
            SaveCarModelCommand = new RelayCommand(_ => SaveCarModel());

            LoadAll();
        }

        // ── Load all lists ────────────────────────────────────────────────────
        private void LoadAll()
        {
            LoadList("http://localhost:5000/api/customers",  Customers);
            LoadList("api/suppliers",  Suppliers);
            LoadList("api/brands",     Brands);
            LoadList("api/parts",      Parts);
            LoadList("api/carmodels",  CarModels);
        }

        private void LoadList<T>(string url, ObservableCollection<T> collection)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest("", Method.Get);

                var response =  client.Execute(request);

                if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                    return;

                var items = JsonSerializer.Deserialize<List<T>>(response.Content, _json) ?? new List<T>();

                collection.Clear();

                foreach (var item in items)
                    collection.Add(item);
            }
            catch
            {
                // API not running — silently skip
            }
        }

        // ── Save helpers ──────────────────────────────────────────────────────
        private void Post(string url, object body, string entityName)
        {
            try
            {
                var content  = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                var response = _http.PostAsync(url, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    Status = $"✓ {entityName} saved successfully.";
                    LoadAll();
                }
                else
                {
                    Status = $"✗ Error saving {entityName}: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                Status = $"✗ {ex.Message}";
            }
        }

        private void SaveCustomer()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName)) { Status = "✗ Customer name is required."; return; }
            Post("api/customers", new {
                Name = NewCustomerName, Phone = NewCustomerPhone, Email = NewCustomerEmail,
                Address = NewCustomerAddress, TaxNumber = NewCustomerTax, OpeningBalance = NewCustomerBalance
            }, "Customer");
            NewCustomerName = NewCustomerPhone = NewCustomerEmail = NewCustomerAddress = NewCustomerTax = string.Empty;
            NewCustomerBalance = 0;
            OnPropertyChanged(nameof(NewCustomerName)); OnPropertyChanged(nameof(NewCustomerPhone));
            OnPropertyChanged(nameof(NewCustomerEmail)); OnPropertyChanged(nameof(NewCustomerAddress));
            OnPropertyChanged(nameof(NewCustomerTax)); OnPropertyChanged(nameof(NewCustomerBalance));
        }

        private void SaveSupplier()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName)) { Status = "✗ Supplier name is required."; return; }
            Post("api/suppliers", new {
                Name = NewSupplierName, Phone = NewSupplierPhone, Email = NewSupplierEmail,
                Address = NewSupplierAddress, TaxNumber = NewSupplierTax, OpeningBalance = NewSupplierBalance
            }, "Supplier");
            NewSupplierName = NewSupplierPhone = NewSupplierEmail = NewSupplierAddress = NewSupplierTax = string.Empty;
            NewSupplierBalance = 0;
            OnPropertyChanged(nameof(NewSupplierName)); OnPropertyChanged(nameof(NewSupplierPhone));
        }

        private void SaveBrand()
        {
            if (string.IsNullOrWhiteSpace(NewBrandName)) { Status = "✗ Brand name is required."; return; }
            Post("api/brands", new { Name = NewBrandName, IsActive = NewBrandIsActive }, "Brand");
            NewBrandName = string.Empty;
            OnPropertyChanged(nameof(NewBrandName));
        }

        private void SavePart()
        {
            if (string.IsNullOrWhiteSpace(NewPartCode) || string.IsNullOrWhiteSpace(NewPartName))
            { Status = "✗ Part code and name are required."; return; }
            Post("api/parts", new {
                InternalCode = NewPartCode, Name = NewPartName, OEMNumber = NewPartOEM,
                Condition = 1, CategoryId = NewPartCategoryId, BrandId = NewPartBrandId,
                CostPrice = NewPartCostPrice, SalePrice = NewPartSalePrice,
                Currency = NewPartCurrency, MinStock = NewPartMinStock, Notes = NewPartNotes
            }, "Part");
            NewPartCode = NewPartName = NewPartOEM = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            OnPropertyChanged(nameof(NewPartCode)); OnPropertyChanged(nameof(NewPartName));
        }

        private void SaveCarModel()
        {
            if (string.IsNullOrWhiteSpace(NewCarModelName)) { Status = "✗ Car model name is required."; return; }
            Post("api/carmodels", new {
                Name = NewCarModelName, Year = NewCarModelYear, EngineType = NewCarModelEngine,
                BasePrice = NewCarModelBasePrice, BrandId = NewCarModelBrandId
            }, "Car Model");
            NewCarModelName = NewCarModelYear = NewCarModelEngine = string.Empty;
            NewCarModelBasePrice = 0;
            OnPropertyChanged(nameof(NewCarModelName)); OnPropertyChanged(nameof(NewCarModelYear));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
