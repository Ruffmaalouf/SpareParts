using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
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

        public CustomerManagementViewModel CustomersFeature { get; } = new();
        public SupplierManagementViewModel SuppliersFeature { get; } = new();
        public BrandManagementViewModel BrandsFeature { get; } = new();
        public PartManagementViewModel PartsFeature { get; } = new();
        public CarModelManagementViewModel CarModelsFeature { get; } = new();

        public UsersViewModel UsersVm { get; } = new();
        public RolesViewModel RolesVm { get; } = new();
        public ObservableCollection<CategoryDto> Categories { get; } = new();

        public ObservableCollection<CustomerDto> Customers => CustomersFeature.Customers;
        public ObservableCollection<SupplierDto> Suppliers => SuppliersFeature.Suppliers;
        public ObservableCollection<BrandDto> Brands => BrandsFeature.Brands;
        public ObservableCollection<PartDto> Parts => PartsFeature.Parts;
        public ObservableCollection<CarModelDto> CarModels => CarModelsFeature.CarModels;
        public ObservableCollection<CarBrandDto> CarBrands => CarModelsFeature.CarBrands;

        public CustomerDto? SelectedCustomer { get => CustomersFeature.SelectedCustomer; set { CustomersFeature.SelectedCustomer = value; OnPropertyChanged(nameof(SelectedCustomer)); if (value != null) { CustomersFeature.PopulateForm(value); RaiseCustomerProps(); } } }
        public SupplierDto? SelectedSupplier { get => SuppliersFeature.SelectedSupplier; set { SuppliersFeature.SelectedSupplier = value; OnPropertyChanged(nameof(SelectedSupplier)); if (value != null) { SuppliersFeature.PopulateForm(value); RaiseSupplierProps(); } } }
        public BrandDto? SelectedBrand { get => BrandsFeature.SelectedBrand; set { BrandsFeature.SelectedBrand = value; OnPropertyChanged(nameof(SelectedBrand)); if (value != null) { BrandsFeature.PopulateForm(value); RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive)); } } }
        public PartDto? SelectedPart { get => PartsFeature.SelectedPart; set { PartsFeature.SelectedPart = value; OnPropertyChanged(nameof(SelectedPart)); if (value != null) { PartsFeature.PopulateForm(value); RaisePartProps(); } } }
        public CarModelDto? SelectedCarModel { get => CarModelsFeature.SelectedCarModel; set { CarModelsFeature.SelectedCarModel = value; OnPropertyChanged(nameof(SelectedCarModel)); if (value != null) { CarModelsFeature.PopulateForm(value); RaiseCarModelProps(); } } }

        public string NewCustomerName { get => CustomersFeature.NewCustomerName; set { CustomersFeature.NewCustomerName = value; OnPropertyChanged(nameof(NewCustomerName)); } }
        public string NewCustomerPhone { get => CustomersFeature.NewCustomerPhone; set { CustomersFeature.NewCustomerPhone = value; OnPropertyChanged(nameof(NewCustomerPhone)); } }
        public string NewCustomerEmail { get => CustomersFeature.NewCustomerEmail; set { CustomersFeature.NewCustomerEmail = value; OnPropertyChanged(nameof(NewCustomerEmail)); } }
        public string NewCustomerAddress { get => CustomersFeature.NewCustomerAddress; set { CustomersFeature.NewCustomerAddress = value; OnPropertyChanged(nameof(NewCustomerAddress)); } }
        public string NewCustomerTax { get => CustomersFeature.NewCustomerTax; set { CustomersFeature.NewCustomerTax = value; OnPropertyChanged(nameof(NewCustomerTax)); } }
        public decimal NewCustomerBalance { get => CustomersFeature.NewCustomerBalance; set { CustomersFeature.NewCustomerBalance = value; OnPropertyChanged(nameof(NewCustomerBalance)); } }

        public string NewSupplierName { get => SuppliersFeature.NewSupplierName; set { SuppliersFeature.NewSupplierName = value; OnPropertyChanged(nameof(NewSupplierName)); } }
        public string NewSupplierPhone { get => SuppliersFeature.NewSupplierPhone; set { SuppliersFeature.NewSupplierPhone = value; OnPropertyChanged(nameof(NewSupplierPhone)); } }
        public string NewSupplierEmail { get => SuppliersFeature.NewSupplierEmail; set { SuppliersFeature.NewSupplierEmail = value; OnPropertyChanged(nameof(NewSupplierEmail)); } }
        public string NewSupplierAddress { get => SuppliersFeature.NewSupplierAddress; set { SuppliersFeature.NewSupplierAddress = value; OnPropertyChanged(nameof(NewSupplierAddress)); } }
        public string NewSupplierTax { get => SuppliersFeature.NewSupplierTax; set { SuppliersFeature.NewSupplierTax = value; OnPropertyChanged(nameof(NewSupplierTax)); } }
        public decimal NewSupplierBalance { get => SuppliersFeature.NewSupplierBalance; set { SuppliersFeature.NewSupplierBalance = value; OnPropertyChanged(nameof(NewSupplierBalance)); } }

        public string NewBrandName { get => BrandsFeature.NewBrandName; set { BrandsFeature.NewBrandName = value; OnPropertyChanged(nameof(NewBrandName)); } }
        public bool NewBrandIsActive { get => BrandsFeature.NewBrandIsActive; set { BrandsFeature.NewBrandIsActive = value; OnPropertyChanged(nameof(NewBrandIsActive)); } }

        public string NewPartCode { get => PartsFeature.NewPartCode; set { PartsFeature.NewPartCode = value; OnPropertyChanged(nameof(NewPartCode)); } }
        public string NewPartName { get => PartsFeature.NewPartName; set { PartsFeature.NewPartName = value; OnPropertyChanged(nameof(NewPartName)); } }
        public string NewPartOEM { get => PartsFeature.NewPartOEM; set { PartsFeature.NewPartOEM = value; OnPropertyChanged(nameof(NewPartOEM)); } }
        public decimal NewPartCostPrice { get => PartsFeature.NewPartCostPrice; set { PartsFeature.NewPartCostPrice = value; OnPropertyChanged(nameof(NewPartCostPrice)); } }
        public decimal NewPartSalePrice { get => PartsFeature.NewPartSalePrice; set { PartsFeature.NewPartSalePrice = value; OnPropertyChanged(nameof(NewPartSalePrice)); } }
        public string NewPartCurrency { get => PartsFeature.NewPartCurrency; set { PartsFeature.NewPartCurrency = value; OnPropertyChanged(nameof(NewPartCurrency)); } }
        public int NewPartMinStock { get => PartsFeature.NewPartMinStock; set { PartsFeature.NewPartMinStock = value; OnPropertyChanged(nameof(NewPartMinStock)); } }
        public int NewPartCategoryId { get => PartsFeature.NewPartCategoryId; set { PartsFeature.NewPartCategoryId = value; OnPropertyChanged(nameof(NewPartCategoryId)); } }
        public int? NewPartBrandId { get => PartsFeature.NewPartBrandId; set { PartsFeature.NewPartBrandId = value; OnPropertyChanged(nameof(NewPartBrandId)); } }
        public string NewPartNotes { get => PartsFeature.NewPartNotes; set { PartsFeature.NewPartNotes = value; OnPropertyChanged(nameof(NewPartNotes)); } }

        public string NewCarModelName { get => CarModelsFeature.NewCarModelName; set { CarModelsFeature.NewCarModelName = value; OnPropertyChanged(nameof(NewCarModelName)); } }
        public string NewCarModelYear { get => CarModelsFeature.NewCarModelYear; set { CarModelsFeature.NewCarModelYear = value; OnPropertyChanged(nameof(NewCarModelYear)); } }
        public string NewCarModelEngine { get => CarModelsFeature.NewCarModelEngine; set { CarModelsFeature.NewCarModelEngine = value; OnPropertyChanged(nameof(NewCarModelEngine)); } }
        public decimal NewCarModelBasePrice { get => CarModelsFeature.NewCarModelBasePrice; set { CarModelsFeature.NewCarModelBasePrice = value; OnPropertyChanged(nameof(NewCarModelBasePrice)); } }
        public int NewCarModelBrandId { get => CarModelsFeature.NewCarModelBrandId; set { CarModelsFeature.NewCarModelBrandId = value; OnPropertyChanged(nameof(NewCarModelBrandId)); } }

        private string _status = string.Empty;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }

        public ICommand LoadAllCommand { get; }
        public ICommand SaveCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand SaveSupplierCommand { get; }
        public ICommand DeleteSupplierCommand { get; }
        public ICommand SaveBrandCommand { get; }
        public ICommand DeleteBrandCommand { get; }
        public ICommand SavePartCommand { get; }
        public ICommand DeletePartCommand { get; }
        public ICommand SaveCarModelCommand { get; }
        public ICommand DeleteCarModelCommand { get; }

        public ManagementViewModel()
        {
            LoadAllCommand = new RelayCommand(_ => _ = LoadAllAsync());
            SaveCustomerCommand = new RelayCommand(_ => _ = SaveCustomerAsync());
            DeleteCustomerCommand = new RelayCommand(_ => _ = DeleteCustomerAsync());
            SaveSupplierCommand = new RelayCommand(_ => _ = SaveSupplierAsync());
            DeleteSupplierCommand = new RelayCommand(_ => _ = DeleteSupplierAsync());
            SaveBrandCommand = new RelayCommand(_ => _ = SaveBrandAsync());
            DeleteBrandCommand = new RelayCommand(_ => _ = DeleteBrandAsync());
            SavePartCommand = new RelayCommand(_ => _ = SavePartAsync());
            DeletePartCommand = new RelayCommand(_ => _ = DeletePartAsync());
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
            DeleteCarModelCommand = new RelayCommand(_ => _ = DeleteCarModelAsync());
        }

        public async Task LoadAllAsync()
        {
            Status = "Loading…";
            try
            {
                var customers = await Api.GetAllAsync<CustomerDto>("api/customers");
                var suppliers = await Api.GetAllAsync<SupplierDto>("api/suppliers");
                var brands = await Api.GetAllAsync<BrandDto>("api/brands");
                var carBrands = await Api.GetCarBrandsAsync();
                var categories = await Api.GetAllAsync<CategoryDto>("api/categories");
                var parts = await Api.GetAllAsync<PartDto>("api/parts");
                var carModels = await Api.GetAllAsync<CarModelDto>("api/carmodels");
                await RolesVm.LoadAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Replace(Customers, customers);
                    Replace(Suppliers, suppliers);
                    Replace(Brands, brands);
                    Replace(CarBrands, carBrands);
                    Replace(Categories, categories);
                    Replace(Parts, parts);
                    Replace(CarModels, carModels);
                });

                Status = "✓ Data loaded.";
            }
            catch (Exception ex) { Status = $"✗ Load failed: {ex.Message}"; }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private async Task SaveCustomerAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCustomerName)) { Status = "✗ Customer name is required."; return; }
            var isEditing = SelectedCustomer is { Id: > 0 };
            await SaveAsync(isEditing ? $"api/customers/{SelectedCustomer!.Id}" : "api/customers", new CreateCustomerRequest
            {
                Name = NewCustomerName,
                Phone = NewCustomerPhone,
                Email = NewCustomerEmail,
                Address = NewCustomerAddress,
                TaxNumber = NewCustomerTax,
                OpeningBalance = NewCustomerBalance
            }, "Customer", isEditing);
            CustomersFeature.ClearForm();
            RaiseCustomerProps();
        }

        private async Task DeleteCustomerAsync()
        {
            if (SelectedCustomer is not { Id: > 0 }) { Status = "✗ Select a customer to delete."; return; }
            await DeleteAsync($"api/customers/{SelectedCustomer.Id}", "Customer");
            CustomersFeature.SelectedCustomer = null;
            OnPropertyChanged(nameof(SelectedCustomer));
        }

        private async Task SaveSupplierAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSupplierName)) { Status = "✗ Supplier name is required."; return; }
            var isEditing = SelectedSupplier is { Id: > 0 };
            await SaveAsync(isEditing ? $"api/suppliers/{SelectedSupplier!.Id}" : "api/suppliers", new CreateSupplierRequest
            {
                Name = NewSupplierName,
                Phone = NewSupplierPhone,
                Email = NewSupplierEmail,
                Address = NewSupplierAddress,
                TaxNumber = NewSupplierTax,
                OpeningBalance = NewSupplierBalance
            }, "Supplier", isEditing);
            SuppliersFeature.ClearForm();
            RaiseSupplierProps();
        }

        private async Task DeleteSupplierAsync()
        {
            if (SelectedSupplier is not { Id: > 0 }) { Status = "✗ Select a supplier to delete."; return; }
            await DeleteAsync($"api/suppliers/{SelectedSupplier.Id}", "Supplier");
            SuppliersFeature.SelectedSupplier = null;
            OnPropertyChanged(nameof(SelectedSupplier));
        }

        private async Task SaveBrandAsync()
        {
            if (string.IsNullOrWhiteSpace(NewBrandName)) { Status = "✗ Brand name is required."; return; }
            var isEditing = SelectedBrand is { Id: > 0 };
            await SaveAsync(isEditing ? $"api/brands/{SelectedBrand!.Id}" : "api/brands", new CreateBrandRequest { Name = NewBrandName, IsActive = NewBrandIsActive }, "Brand", isEditing);
            BrandsFeature.ClearForm();
            RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive), nameof(SelectedBrand));
        }

        private async Task DeleteBrandAsync()
        {
            if (SelectedBrand is not { Id: > 0 }) { Status = "✗ Select a brand to delete."; return; }
            await DeleteAsync($"api/brands/{SelectedBrand.Id}", "Brand");
            BrandsFeature.SelectedBrand = null;
            OnPropertyChanged(nameof(SelectedBrand));
        }

        private async Task SavePartAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPartCode) || string.IsNullOrWhiteSpace(NewPartName)) { Status = "✗ Part code and name are required."; return; }
            var isEditing = SelectedPart is { Id: > 0 };
            await SaveAsync(isEditing ? $"api/parts/{SelectedPart!.Id}" : "api/parts", new CreatePartRequest
            {
                InternalCode = NewPartCode,
                Name = NewPartName,
                OEMNumber = NewPartOEM,
                Condition = PartCondition.New,
                CategoryId = NewPartCategoryId,
                BrandId = NewPartBrandId,
                CostPrice = NewPartCostPrice,
                SalePrice = NewPartSalePrice,
                Currency = NewPartCurrency,
                MinStock = NewPartMinStock,
                Notes = NewPartNotes
            }, "Part", isEditing);
            PartsFeature.ClearForm();
            RaisePartProps();
        }

        private async Task DeletePartAsync()
        {
            if (SelectedPart is not { Id: > 0 }) { Status = "✗ Select a part to delete."; return; }
            await DeleteAsync($"api/parts/{SelectedPart.Id}", "Part");
            PartsFeature.SelectedPart = null;
            OnPropertyChanged(nameof(SelectedPart));
        }

        private async Task SaveCarModelAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCarModelName)) { Status = "✗ Car model name is required."; return; }
            var isEditing = SelectedCarModel is { Id: > 0 };
            await SaveAsync(isEditing ? $"api/carmodels/{SelectedCarModel!.Id}" : "api/carmodels", new CreateCarModelRequest
            {
                Name = NewCarModelName,
                Year = NewCarModelYear,
                EngineType = NewCarModelEngine,
                BasePrice = NewCarModelBasePrice,
                CarBrandId = NewCarModelBrandId
            }, "Car Model", isEditing);
            CarModelsFeature.ClearForm();
            RaiseCarModelProps();
        }

        private async Task DeleteCarModelAsync()
        {
            if (SelectedCarModel is not { Id: > 0 }) { Status = "✗ Select a car model to delete."; return; }
            await DeleteAsync($"api/carmodels/{SelectedCarModel.Id}", "Car model");
            CarModelsFeature.SelectedCarModel = null;
            OnPropertyChanged(nameof(SelectedCarModel));
        }

        private async Task SaveAsync(string url, object payload, string entityName, bool isEditing)
        {
            try
            {
                if (isEditing) { await Api.PutAsync(url, payload); Status = $"✓ {entityName} updated."; }
                else { await Api.PostAsync(url, payload); Status = $"✓ {entityName} saved."; }
                await LoadAllAsync();
            }
            catch (Exception ex) { Status = $"✗ Error saving {entityName}: {ex.Message}"; }
        }

        private async Task DeleteAsync(string url, string entityName)
        {
            try
            {
                await Api.DeleteAsync(url);
                Status = $"✓ {entityName} deleted.";
                await LoadAllAsync();
            }
            catch (Exception ex) { Status = $"✗ Error deleting {entityName}: {ex.Message}"; }
        }

        private void RaiseCustomerProps() => RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail), nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        private void RaiseSupplierProps() => RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail), nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        private void RaisePartProps() => RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM), nameof(NewPartCategoryId), nameof(NewPartBrandId), nameof(NewPartCostPrice), nameof(NewPartSalePrice), nameof(NewPartCurrency), nameof(NewPartMinStock), nameof(NewPartNotes));
        private void RaiseCarModelProps() => RaiseAll(nameof(NewCarModelBrandId), nameof(NewCarModelName), nameof(NewCarModelYear), nameof(NewCarModelEngine), nameof(NewCarModelBasePrice));

        private void RaiseAll(params string[] names) { foreach (var n in names) OnPropertyChanged(n); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
