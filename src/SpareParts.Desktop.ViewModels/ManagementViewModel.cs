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
using System.Windows.Media;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public class ManagementViewModel : INotifyPropertyChanged
    {
        private readonly ManagementCoordinator _coordinator;

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

        public string NewCarBrandName { get => CarModelsFeature.NewCarBrandName; set { CarModelsFeature.NewCarBrandName = value; OnPropertyChanged(nameof(NewCarBrandName)); } }
        public string NewCarBrandCountry { get => CarModelsFeature.NewCarBrandCountry; set { CarModelsFeature.NewCarBrandCountry = value; OnPropertyChanged(nameof(NewCarBrandCountry)); } }
        public string NewCarBrandRegionGroup { get => CarModelsFeature.NewCarBrandRegionGroup; set { CarModelsFeature.NewCarBrandRegionGroup = value; OnPropertyChanged(nameof(NewCarBrandRegionGroup)); } }
        public int NewCarBrandSortOrder { get => CarModelsFeature.NewCarBrandSortOrder; set { CarModelsFeature.NewCarBrandSortOrder = value; OnPropertyChanged(nameof(NewCarBrandSortOrder)); } }

        public string NewCarModelName { get => CarModelsFeature.NewCarModelName; set { CarModelsFeature.NewCarModelName = value; OnPropertyChanged(nameof(NewCarModelName)); } }
        public string NewCarModelYear { get => CarModelsFeature.NewCarModelYear; set { CarModelsFeature.NewCarModelYear = value; OnPropertyChanged(nameof(NewCarModelYear)); } }
        public string NewCarModelEngine { get => CarModelsFeature.NewCarModelEngine; set { CarModelsFeature.NewCarModelEngine = value; OnPropertyChanged(nameof(NewCarModelEngine)); } }
        public decimal NewCarModelBasePrice { get => CarModelsFeature.NewCarModelBasePrice; set { CarModelsFeature.NewCarModelBasePrice = value; OnPropertyChanged(nameof(NewCarModelBasePrice)); } }
        public int NewCarModelBrandId { get => CarModelsFeature.NewCarModelBrandId; set { CarModelsFeature.NewCarModelBrandId = value; OnPropertyChanged(nameof(NewCarModelBrandId)); } }

        private string _status = string.Empty;
        public string Status { get => _status; set { _status = value; OnPropertyChanged(nameof(Status)); } }
        public ObservableCollection<StatusMessage> StatusMessages { get; } = new();

        private Brush _statusBrush = Brushes.DodgerBlue;
        public Brush StatusBrush { get => _statusBrush; set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); } }

        public ICommand LoadAllCommand { get; }
        public ICommand SaveCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand SaveSupplierCommand { get; }
        public ICommand DeleteSupplierCommand { get; }
        public ICommand SaveBrandCommand { get; }
        public ICommand DeleteBrandCommand { get; }
        public ICommand SavePartCommand { get; }
        public ICommand DeletePartCommand { get; }
        public ICommand SaveCarBrandCommand { get; }
        public ICommand SaveCarModelCommand { get; }
        public ICommand DeleteCarModelCommand { get; }

        public ManagementViewModel(ICrudApiClient? crudApi = null, ICarCatalogApiClient? carCatalogApi = null)
        {
            _coordinator = new ManagementCoordinator(crudApi ?? new CrudApiClient(), carCatalogApi ?? new CarCatalogApiClient());

            LoadAllCommand = new RelayCommand(_ => _ = LoadAllAsync());
            SaveCustomerCommand = new RelayCommand(_ => _ = SaveCustomerAsync());
            DeleteCustomerCommand = new RelayCommand(_ => _ = DeleteCustomerAsync());
            SaveSupplierCommand = new RelayCommand(_ => _ = SaveSupplierAsync());
            DeleteSupplierCommand = new RelayCommand(_ => _ = DeleteSupplierAsync());
            SaveBrandCommand = new RelayCommand(_ => _ = SaveBrandAsync());
            DeleteBrandCommand = new RelayCommand(_ => _ = DeleteBrandAsync());
            SavePartCommand = new RelayCommand(_ => _ = SavePartAsync());
            DeletePartCommand = new RelayCommand(_ => _ = DeletePartAsync());
            SaveCarBrandCommand = new RelayCommand(_ => _ = SaveCarBrandAsync());
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
            DeleteCarModelCommand = new RelayCommand(_ => _ = DeleteCarModelAsync());
        }

        public async Task LoadAllAsync()
        {
            SetStatus("Loading…", true);
            try
            {
                var loadResult = await _coordinator.LoadAllAsync(RolesVm);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Replace(Customers, loadResult.Customers);
                    Replace(Suppliers, loadResult.Suppliers);
                    Replace(Brands, loadResult.Brands);
                    Replace(CarBrands, loadResult.CarBrands);
                    Replace(Categories, loadResult.Categories);
                    Replace(Parts, loadResult.Parts);
                    Replace(CarModels, loadResult.CarModels);
                });

                SetStatus("✓ Data loaded.", true);
            }
            catch (Exception ex)
            {
                var message = ex is ApiClientException apiException
                    ? $"✗ API error ({apiException.Code}): {apiException.Message}"
                    : "✗ Load failed due to an unexpected error.";
                SetStatus(message, false);
            }
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
            var result = await _coordinator.SaveCustomerAsync(CustomersFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CustomersFeature.ClearForm();
            RaiseCustomerProps();
        }

        private async Task DeleteCustomerAsync()
        {
            var result = await _coordinator.DeleteCustomerAsync(SelectedCustomer);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CustomersFeature.SelectedCustomer = null;
            OnPropertyChanged(nameof(SelectedCustomer));
        }

        private async Task SaveSupplierAsync()
        {
            var result = await _coordinator.SaveSupplierAsync(SuppliersFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SuppliersFeature.ClearForm();
            RaiseSupplierProps();
        }

        private async Task DeleteSupplierAsync()
        {
            var result = await _coordinator.DeleteSupplierAsync(SelectedSupplier);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SuppliersFeature.SelectedSupplier = null;
            OnPropertyChanged(nameof(SelectedSupplier));
        }

        private async Task SaveBrandAsync()
        {
            var result = await _coordinator.SaveBrandAsync(BrandsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            BrandsFeature.ClearForm();
            RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive), nameof(SelectedBrand));
        }

        private async Task DeleteBrandAsync()
        {
            var result = await _coordinator.DeleteBrandAsync(SelectedBrand);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            BrandsFeature.SelectedBrand = null;
            OnPropertyChanged(nameof(SelectedBrand));
        }

        private async Task SavePartAsync()
        {
            var result = await _coordinator.SavePartAsync(PartsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            PartsFeature.ClearForm();
            RaisePartProps();
        }

        private async Task DeletePartAsync()
        {
            var result = await _coordinator.DeletePartAsync(SelectedPart);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            PartsFeature.SelectedPart = null;
            OnPropertyChanged(nameof(SelectedPart));
        }

        private async Task SaveCarBrandAsync()
        {
            var result = await _coordinator.SaveCarBrandAsync(CarModelsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CarModelsFeature.ClearCarBrandForm();
            RaiseCarBrandProps();
        }

        private async Task SaveCarModelAsync()
        {
            var result = await _coordinator.SaveCarModelAsync(CarModelsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CarModelsFeature.ClearForm();
            RaiseCarModelProps();
        }

        private async Task DeleteCarModelAsync()
        {
            var result = await _coordinator.DeleteCarModelAsync(SelectedCarModel);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CarModelsFeature.SelectedCarModel = null;
            OnPropertyChanged(nameof(SelectedCarModel));
        }

        private void RaiseCustomerProps() => RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail), nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        private void RaiseSupplierProps() => RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail), nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        private void RaisePartProps() => RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM), nameof(NewPartCategoryId), nameof(NewPartBrandId), nameof(NewPartCostPrice), nameof(NewPartSalePrice), nameof(NewPartCurrency), nameof(NewPartMinStock), nameof(NewPartNotes));
        private void RaiseCarBrandProps() => RaiseAll(nameof(NewCarBrandName), nameof(NewCarBrandCountry), nameof(NewCarBrandRegionGroup), nameof(NewCarBrandSortOrder));
        private void RaiseCarModelProps() => RaiseAll(nameof(NewCarModelBrandId), nameof(NewCarModelName), nameof(NewCarModelYear), nameof(NewCarModelEngine), nameof(NewCarModelBasePrice));
        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.MediumSeaGreen : Brushes.IndianRed;
            StatusMessages.Insert(0, new StatusMessage { Text = message, IsSuccess = isSuccess });
            AppNotificationCenter.Instance.Publish(message, isSuccess);
            const int maxMessages = 8;
            while (StatusMessages.Count > maxMessages)
            {
                StatusMessages.RemoveAt(StatusMessages.Count - 1);
            }
        }

        private void RaiseAll(params string[] names) { foreach (var n in names) OnPropertyChanged(n); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
