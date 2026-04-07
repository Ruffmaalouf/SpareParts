using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public class ManagementViewModel : INotifyPropertyChanged
    {
        private readonly ManagementCoordinator _coordinator;
        private readonly ICrudApiClient _crudApi;
        private readonly SupplierPermissionState _supplierPermissions = new();
        private readonly ManagementStatusCenter _statusCenter = new();
        private string _baseCurrencyCode = "USD";
        private string _counterCurrencyCode = "USD";

        public CustomerManagementViewModel CustomersFeature { get; } = new();
        public SupplierManagementViewModel SuppliersFeature { get; } = new();
        public BrandManagementViewModel BrandsFeature { get; } = new();
        public PartManagementViewModel PartsFeature { get; } = new();
        public CarModelManagementViewModel CarModelsFeature { get; } = new();
        public WarehouseManagementViewModel WarehousesFeature { get; } = new();
        public TransactionTypeManagementViewModel TransactionTypesFeature { get; } = new();

        public UsersViewModel UsersVm { get; }
        public RolesViewModel RolesVm { get; }
        public ObservableCollection<CategoryDto> Categories { get; } = new();

        public ObservableCollection<CustomerDto> Customers => CustomersFeature.Customers;
        public ObservableCollection<SupplierDto> Suppliers => SuppliersFeature.Suppliers;
        public ObservableCollection<BrandDto> Brands => BrandsFeature.Brands;
        public ObservableCollection<PartDto> Parts => PartsFeature.Parts;
        public ObservableCollection<CarModelDto> CarModels => CarModelsFeature.CarModels;
        public ObservableCollection<CarBrandDto> CarBrands => CarModelsFeature.CarBrands;
        public ObservableCollection<WarehouseDto> Warehouses => WarehousesFeature.Warehouses;
        public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = new();
        public ObservableCollection<string> UsedCarCurrencyCodes { get; } = new();
        public ObservableCollection<TransactionTypeDto> TransactionTypes => TransactionTypesFeature.TransactionTypes;
        public ObservableCollection<UsedCarEntry> UsedCars { get; } = new();


        public bool CanViewSupplierTab => _supplierPermissions.CanViewSupplierTab;
        public bool CanViewCurrencyTab
        {
            get => _canViewCurrencyTab;
            private set
            {
                if (_canViewCurrencyTab == value) return;
                _canViewCurrencyTab = value;
                OnPropertyChanged(nameof(CanViewCurrencyTab));
            }
        }
        public bool CanViewTransactionTypesTab
        {
            get => _canViewTransactionTypesTab;
            private set
            {
                if (_canViewTransactionTypesTab == value) return;
                _canViewTransactionTypesTab = value;
                OnPropertyChanged(nameof(CanViewTransactionTypesTab));
            }
        }
        public bool CanEditSupplier => _supplierPermissions.CanEditSupplier;
        public bool CanModifySupplier => _supplierPermissions.CanModifySupplier;
        public bool CanDeleteSupplier => _supplierPermissions.CanDeleteSupplier;
        public bool CanSaveSupplier => _supplierPermissions.CanSaveSupplier;

        public CustomerDto? SelectedCustomer { get => CustomersFeature.SelectedCustomer; set { CustomersFeature.SelectedCustomer = value; OnPropertyChanged(nameof(SelectedCustomer)); if (value != null) { CustomersFeature.PopulateForm(value); RaiseCustomerProps(); } } }
        public SupplierDto? SelectedSupplier
        {
            get => SuppliersFeature.SelectedSupplier;
            set
            {
                SuppliersFeature.SelectedSupplier = value;
                _supplierPermissions.IsEditingSupplier = value != null;
                OnPropertyChanged(nameof(SelectedSupplier));
                OnPropertyChanged(nameof(CanSaveSupplier));
                if (value != null) { SuppliersFeature.PopulateForm(value); RaiseSupplierProps(); }
            }
        }
        public BrandDto? SelectedBrand { get => BrandsFeature.SelectedBrand; set { BrandsFeature.SelectedBrand = value; OnPropertyChanged(nameof(SelectedBrand)); if (value != null) { BrandsFeature.PopulateForm(value); RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive)); } } }
        public PartDto? SelectedPart { get => PartsFeature.SelectedPart; set { PartsFeature.SelectedPart = value; OnPropertyChanged(nameof(SelectedPart)); if (value != null) { PartsFeature.PopulateForm(value); RaisePartProps(); } } }
        public CarModelDto? SelectedCarModel { get => CarModelsFeature.SelectedCarModel; set { CarModelsFeature.SelectedCarModel = value; OnPropertyChanged(nameof(SelectedCarModel)); if (value != null) { CarModelsFeature.PopulateForm(value); RaiseCarModelProps(); } } }
        public WarehouseDto? SelectedWarehouse { get => WarehousesFeature.SelectedWarehouse; set { WarehousesFeature.SelectedWarehouse = value; OnPropertyChanged(nameof(SelectedWarehouse)); if (value != null) { WarehousesFeature.PopulateForm(value); RaiseWarehouseProps(); } } }
        public TransactionTypeDto? SelectedTransactionType
        {
            get => TransactionTypesFeature.SelectedTransactionType;
            set
            {
                TransactionTypesFeature.SelectedTransactionType = value;
                OnPropertyChanged(nameof(SelectedTransactionType));
                if (value != null)
                {
                    TransactionTypesFeature.PopulateForm(value);
                    RaiseTransactionTypeProps();
                }
            }
        }

        public UsedCarEntry? SelectedUsedCar
        {
            get => _selectedUsedCar;
            set
            {
                _selectedUsedCar = value;
                OnPropertyChanged(nameof(SelectedUsedCar));
                if (value != null)
                {
                    NewUsedCarName = value.Car;
                    NewUsedCarModelYear = value.ModelYear;
                    NewUsedCarPriceCurrency = value.PriceCurrency;
                    NewUsedCarPriceBase = value.PriceBase;
                    NewUsedCarPriceCounter = value.PriceCounter;
                    NewUsedCarLocation = value.Location;
                    NewUsedCarTransportation = value.Transportation;
                    NewUsedCarPartOut = value.PartOut;
                    NewUsedCarShipping = value.Shipping;
                    NewUsedCarCustoms = value.Customs;
                    NewUsedCarCarModelId = CarModels
                        .FirstOrDefault(m =>
                            string.Equals(m.Name, value.Car, StringComparison.OrdinalIgnoreCase)
                            && int.TryParse(m.Year, out var modelYear)
                            && modelYear == value.ModelYear)?.Id;
                }
            }
        }

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
        public string NewWarehouseName { get => WarehousesFeature.NewWarehouseName; set { WarehousesFeature.NewWarehouseName = value; OnPropertyChanged(nameof(NewWarehouseName)); } }
        public string NewWarehouseAddress { get => WarehousesFeature.NewWarehouseAddress; set { WarehousesFeature.NewWarehouseAddress = value; OnPropertyChanged(nameof(NewWarehouseAddress)); } }
        public bool NewWarehouseIsMain { get => WarehousesFeature.NewWarehouseIsMain; set { WarehousesFeature.NewWarehouseIsMain = value; OnPropertyChanged(nameof(NewWarehouseIsMain)); } }
        public string NewTransactionTypeName { get => TransactionTypesFeature.NewTransactionTypeName; set { TransactionTypesFeature.NewTransactionTypeName = value; OnPropertyChanged(nameof(NewTransactionTypeName)); } }
        public string NewTransactionCurrencyCode { get => TransactionTypesFeature.NewTransactionCurrencyCode; set { TransactionTypesFeature.NewTransactionCurrencyCode = value; OnPropertyChanged(nameof(NewTransactionCurrencyCode)); } }
        public decimal NewTransactionCounterRate { get => TransactionTypesFeature.NewTransactionCounterRate; set { TransactionTypesFeature.NewTransactionCounterRate = value; OnPropertyChanged(nameof(NewTransactionCounterRate)); } }
        public bool NewTransactionIsActive { get => TransactionTypesFeature.NewTransactionIsActive; set { TransactionTypesFeature.NewTransactionIsActive = value; OnPropertyChanged(nameof(NewTransactionIsActive)); } }

        public string NewUsedCarName
        {
            get => _newUsedCarName;
            set
            {
                _newUsedCarName = value;
                OnPropertyChanged(nameof(NewUsedCarName));
            }
        }

        public int NewUsedCarModelYear
        {
            get => _newUsedCarModelYear;
            set
            {
                _newUsedCarModelYear = value;
                OnPropertyChanged(nameof(NewUsedCarModelYear));
            }
        }

        public int? NewUsedCarCarModelId
        {
            get => _newUsedCarCarModelId;
            set
            {
                _newUsedCarCarModelId = value;
                OnPropertyChanged(nameof(NewUsedCarCarModelId));
                SyncUsedCarFromSelectedModel();
            }
        }

        public string NewUsedCarPriceCurrency
        {
            get => _newUsedCarPriceCurrency;
            set
            {
                _newUsedCarPriceCurrency = value;
                OnPropertyChanged(nameof(NewUsedCarPriceCurrency));
            }
        }

        public decimal NewUsedCarPriceBase
        {
            get => _newUsedCarPriceBase;
            set
            {
                _newUsedCarPriceBase = value;
                OnPropertyChanged(nameof(NewUsedCarPriceBase));
            }
        }

        public decimal NewUsedCarPriceCounter
        {
            get => _newUsedCarPriceCounter;
            set
            {
                _newUsedCarPriceCounter = value;
                OnPropertyChanged(nameof(NewUsedCarPriceCounter));
            }
        }

        public string NewUsedCarLocation
        {
            get => _newUsedCarLocation;
            set
            {
                _newUsedCarLocation = value;
                OnPropertyChanged(nameof(NewUsedCarLocation));
            }
        }

        public decimal NewUsedCarTransportation
        {
            get => _newUsedCarTransportation;
            set
            {
                _newUsedCarTransportation = value;
                OnPropertyChanged(nameof(NewUsedCarTransportation));
            }
        }

        public string NewUsedCarPartOut
        {
            get => _newUsedCarPartOut;
            set
            {
                _newUsedCarPartOut = value;
                OnPropertyChanged(nameof(NewUsedCarPartOut));
            }
        }

        public decimal NewUsedCarShipping
        {
            get => _newUsedCarShipping;
            set
            {
                _newUsedCarShipping = value;
                OnPropertyChanged(nameof(NewUsedCarShipping));
            }
        }

        public decimal NewUsedCarCustoms
        {
            get => _newUsedCarCustoms;
            set
            {
                _newUsedCarCustoms = value;
                OnPropertyChanged(nameof(NewUsedCarCustoms));
            }
        }

        public string Status => _statusCenter.Status;
        public ObservableCollection<StatusMessage> StatusMessages => _statusCenter.StatusMessages;
        public Brush StatusBrush => _statusCenter.StatusBrush;
        private bool _isLoading;
        private UsedCarEntry? _selectedUsedCar;
        private string _newUsedCarName = string.Empty;
        private int _newUsedCarModelYear;
        private int? _newUsedCarCarModelId;
        private string _newUsedCarPriceCurrency = "USD";
        private decimal _newUsedCarPriceBase;
        private decimal _newUsedCarPriceCounter;
        private string _newUsedCarLocation = string.Empty;
        private decimal _newUsedCarTransportation;
        private string _newUsedCarPartOut = string.Empty;
        private decimal _newUsedCarShipping;
        private decimal _newUsedCarCustoms;
        private bool _canViewCurrencyTab;
        private bool _canViewTransactionTypesTab;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

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
        public ICommand SaveWarehouseCommand { get; }
        public ICommand DeleteWarehouseCommand { get; }
        public ICommand SaveTransactionTypeCommand { get; }
        public ICommand DeleteTransactionTypeCommand { get; }
        public ICommand AddUsedCarCommand { get; }
        public ICommand RemoveUsedCarCommand { get; }

        public ManagementViewModel(
            ICrudApiClient crudApi,
            ICarCatalogApiClient carCatalogApi,
            UsersViewModel usersVm,
            RolesViewModel rolesVm,
            bool canViewSupplierTab = false,
            bool canEditSupplier = false,
            bool canModifySupplier = false,
            bool canDeleteSupplier = false)
        {
            UsersVm = usersVm;
            RolesVm = rolesVm;
            SetSupplierPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);
            _crudApi = crudApi;

            _coordinator = new ManagementCoordinator(
                crudApi,
                carCatalogApi);

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
            SaveWarehouseCommand = new RelayCommand(_ => _ = SaveWarehouseAsync());
            DeleteWarehouseCommand = new RelayCommand(_ => _ = DeleteWarehouseAsync());
            SaveTransactionTypeCommand = new RelayCommand(_ => _ = SaveTransactionTypeAsync());
            DeleteTransactionTypeCommand = new RelayCommand(_ => _ = DeleteTransactionTypeAsync());
            AddUsedCarCommand = new RelayCommand(_ => AddUsedCar());
            RemoveUsedCarCommand = new RelayCommand(_ => RemoveSelectedUsedCar());

            SeedUsedCars();
        }

        public void SetTabPermissions(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier, bool canViewCurrencyTab, bool canViewTransactionTypesTab)
        {
            _supplierPermissions.Set(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);
            CanViewCurrencyTab = canViewCurrencyTab;
            CanViewTransactionTypesTab = canViewTransactionTypesTab;
        }

        public void SetSupplierPermissions(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier)
        {
            SetTabPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier, CanViewCurrencyTab, CanViewTransactionTypesTab);
        }
 

        public async Task LoadAllAsync()
        {
            IsLoading = true;
            SetStatus("Loading…", true);
            try
            {
                var loadResult = await _coordinator.LoadAllAsync(RolesVm);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyCurrencyDefaults(loadResult.AppConstants);
                    Replace(Customers, loadResult.Customers);
                    Replace(Suppliers, loadResult.Suppliers);
                    Replace(Brands, loadResult.Brands);
                    Replace(CarBrands, loadResult.CarBrands);
                    Replace(Categories, loadResult.Categories);
                    Replace(Parts, loadResult.Parts);
                    Replace(CarModels, loadResult.CarModels);
                    Replace(Warehouses, loadResult.Warehouses);
                    Replace(CurrencyRates, loadResult.CurrencyRates);
                    ReplaceUsedCarCurrencyCodes();
                    Replace(TransactionTypes, loadResult.TransactionTypes);
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
            finally
            {
                IsLoading = false;
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

        private void ApplyCurrencyDefaults(IEnumerable<AppConstantDto> constants)
        {
            var byKey = constants.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            _baseCurrencyCode = ResolveCurrencyCode(byKey, "BaseCurrencyCode")
                ?? ResolveCurrencyCode(byKey, "DefaultCurrencyCode")
                ?? "USD";
            _counterCurrencyCode = ResolveCurrencyCode(byKey, "CounterCurrencyCode")
                ?? _baseCurrencyCode;
        }

        private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> constants, string key)
        {
            if (!constants.TryGetValue(key, out var value)
                || string.IsNullOrWhiteSpace(value)
                || value.Trim().Length != 3)
            {
                return null;
            }

            return value.Trim().ToUpperInvariant();
        }

        private void ReplaceUsedCarCurrencyCodes()
        {
            var codes = CurrencyRates
                .Select(rate => rate.CurrencyCode?.Trim().ToUpperInvariant())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!codes.Contains(_baseCurrencyCode, StringComparer.OrdinalIgnoreCase))
            {
                codes.Insert(0, _baseCurrencyCode);
            }

            if (!codes.Contains(_counterCurrencyCode, StringComparer.OrdinalIgnoreCase))
            {
                codes.Add(_counterCurrencyCode);
            }

            Replace(UsedCarCurrencyCodes, codes!);
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
            if (!CanViewSupplierTab)
            {
                SetStatus("✗ You do not have permission to view the supplier tab.", false);
                return;
            }

            var isEditing = SelectedSupplier != null;
            if (!isEditing && !CanEditSupplier)
            {
                SetStatus("✗ You do not have permission to create suppliers.", false);
                return;
            }

            if (isEditing && !CanModifySupplier)
            {
                SetStatus("✗ You do not have permission to modify suppliers.", false);
                return;
            }

            var result = await _coordinator.SaveSupplierAsync(SuppliersFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SuppliersFeature.ClearForm();
            RaiseSupplierProps();
            OnPropertyChanged(nameof(CanSaveSupplier));
        }

        private async Task DeleteSupplierAsync()
        {
            if (!CanViewSupplierTab)
            {
                SetStatus("✗ You do not have permission to view the supplier tab.", false);
                return;
            }

            if (!CanDeleteSupplier)
            {
                SetStatus("✗ You do not have permission to delete suppliers.", false);
                return;
            }

            var result = await _coordinator.DeleteSupplierAsync(SelectedSupplier);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SuppliersFeature.SelectedSupplier = null;
            _supplierPermissions.IsEditingSupplier = false;
            OnPropertyChanged(nameof(SelectedSupplier));
            OnPropertyChanged(nameof(CanSaveSupplier));
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
            PartsFeature.ClearForm(_baseCurrencyCode);
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
            if (string.IsNullOrWhiteSpace(NewCarBrandName))
            {
                SetStatus("✗ Car brand name is required.", false);
                return;
            }

            try
            {
                var payload = new CreateCarBrandRequest
                {
                    Name = NewCarBrandName,
                    Country = NewCarBrandCountry,
                    RegionGroup = NewCarBrandRegionGroup,
                    SortOrder = NewCarBrandSortOrder
                };

                await _crudApi.PostAsync("api/carbrands", payload);
                SetStatus("✓ Car Brand saved.", true);
                await LoadAllAsync();
                CarModelsFeature.ClearCarBrandForm();
                RaiseCarBrandProps();
            }
            catch (Exception ex)
            {
                var message = ex is ApiClientException apiException
                    ? $"✗ API error ({apiException.Code}): {apiException.Message}"
                    : "✗ Unexpected error while saving Car Brand.";
                SetStatus(message, false);
            }
 
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

        private async Task SaveWarehouseAsync()
        {
            //var result = await _coordinator.SaveWarehouseAsync(WarehousesFeature);
            //SetStatus(result.Message, result.Success);
            //if (!result.Success) return;

            //await LoadAllAsync();
            //WarehousesFeature.ClearForm();
            //RaiseWarehouseProps();
        }

        private async Task DeleteWarehouseAsync()
        {
            //var result = await _coordinator.DeleteWarehouseAsync(SelectedWarehouse);
            //SetStatus(result.Message, result.Success);
            //if (!result.Success) return;

            //await LoadAllAsync();
            //WarehousesFeature.SelectedWarehouse = null;
            //OnPropertyChanged(nameof(SelectedWarehouse));
        }

        private async Task SaveTransactionTypeAsync()
        {
            var result = await _coordinator.SaveTransactionTypeAsync(TransactionTypesFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            TransactionTypesFeature.ClearForm(_counterCurrencyCode);
            RaiseTransactionTypeProps();
        }

        private async Task DeleteTransactionTypeAsync()
        {
            var result = await _coordinator.DeleteTransactionTypeAsync(SelectedTransactionType);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            TransactionTypesFeature.SelectedTransactionType = null;
            OnPropertyChanged(nameof(SelectedTransactionType));
        }

        private void AddUsedCar()
        {
            if (string.IsNullOrWhiteSpace(NewUsedCarName) || NewUsedCarModelYear <= 0)
            {
                SetStatus("✗ Car and model year are required for used cars.", false);
                return;
            }
            if (SelectedUsedCar == null)
            {
                UsedCars.Add(new UsedCarEntry());
                SelectedUsedCar = UsedCars[^1];
            }

            SelectedUsedCar.Car = NewUsedCarName.Trim();
            SelectedUsedCar.ModelYear = NewUsedCarModelYear;
            SelectedUsedCar.PriceCurrency = string.IsNullOrWhiteSpace(NewUsedCarPriceCurrency) ? "USD" : NewUsedCarPriceCurrency.Trim().ToUpperInvariant();
            SelectedUsedCar.PriceBase = NewUsedCarPriceBase;
            SelectedUsedCar.PriceCounter = NewUsedCarPriceCounter;
            SelectedUsedCar.Location = NewUsedCarLocation.Trim();
            SelectedUsedCar.Transportation = NewUsedCarTransportation;
            SelectedUsedCar.PartOut = NewUsedCarPartOut.Trim();
            SelectedUsedCar.Shipping = NewUsedCarShipping;
            SelectedUsedCar.Customs = NewUsedCarCustoms;

            SetStatus("✓ Used car entry saved.", true);
        }

        private void RemoveSelectedUsedCar()
        {
            if (SelectedUsedCar == null)
            {
                SetStatus("✗ Select a used car row first.", false);
                return;
            }

            UsedCars.Remove(SelectedUsedCar);
            SelectedUsedCar = null;
            ClearUsedCarForm();
            SetStatus("✓ Used car entry removed.", true);
        }

        private void ClearUsedCarForm()
        {
            NewUsedCarName = string.Empty;
            NewUsedCarModelYear = 0;
            NewUsedCarCarModelId = null;
            NewUsedCarPriceCurrency = _baseCurrencyCode;
            NewUsedCarPriceBase = 0;
            NewUsedCarPriceCounter = 0;
            NewUsedCarLocation = string.Empty;
            NewUsedCarTransportation = 0;
            NewUsedCarPartOut = string.Empty;
            NewUsedCarShipping = 0;
            NewUsedCarCustoms = 0;
        }

        private void SyncUsedCarFromSelectedModel()
        {
            if (NewUsedCarCarModelId is not int carModelId)
            {
                return;
            }

            var selectedModel = CarModels.FirstOrDefault(model => model.Id == carModelId);
            if (selectedModel == null)
            {
                return;
            }

            NewUsedCarName = selectedModel.Name;
            if (int.TryParse(selectedModel.Year, out var modelYear))
            {
                NewUsedCarModelYear = modelYear;
            }
        }

        private void SeedUsedCars()
        {
            UsedCars.Clear();
            UsedCars.Add(new UsedCarEntry { Car = "Bmw 335", ModelYear = 2009, PriceCurrency = "CAD", PriceBase = 2660m, PriceCounter = 1915.2m, Location = "CALGARY", Transportation = 200m });
            UsedCars.Add(new UsedCarEntry { Car = "Bmw 535", ModelYear = 2010, PriceCurrency = "CAD", PriceBase = 1800m, PriceCounter = 1296m, Location = "CALGARY", Transportation = 200m });
            UsedCars.Add(new UsedCarEntry { Car = "Bmw 650", ModelYear = 2009, PriceCurrency = "CAD", PriceBase = 1600m, PriceCounter = 1152m, Location = "CALGARY", Transportation = 200m });
            UsedCars.Add(new UsedCarEntry { Car = "Bmw 750", ModelYear = 2012, PriceCurrency = "CAD", PriceBase = 3400m, PriceCounter = 2448m, Location = "EDMONTON", Transportation = 120m });
            UsedCars.Add(new UsedCarEntry { Car = "Bmw X5 4.8", ModelYear = 2008, PriceCurrency = "CAD", PriceBase = 1400m, PriceCounter = 1008m, Location = "EDMONTON", Transportation = 120m });
            UsedCars.Add(new UsedCarEntry { Car = "Mercedes C230", ModelYear = 2009, PriceCurrency = "CAD", PriceBase = 1400m, PriceCounter = 1008m, Location = "EDMONTON", Transportation = 120m });
            UsedCars.Add(new UsedCarEntry { Car = "Audi Q7", ModelYear = 2008, PriceCurrency = "CAD", PriceBase = 2150m, PriceCounter = 1548m, Location = "EDMONTON", Transportation = 120m });
        }

        private void RaiseCustomerProps() => RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail), nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        private void RaiseSupplierProps() => RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail), nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        private void RaisePartProps() => RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM), nameof(NewPartCategoryId), nameof(NewPartBrandId), nameof(NewPartCostPrice), nameof(NewPartSalePrice), nameof(NewPartCurrency), nameof(NewPartMinStock), nameof(NewPartNotes));
        private void RaiseCarBrandProps() => RaiseAll(nameof(NewCarBrandName), nameof(NewCarBrandCountry), nameof(NewCarBrandRegionGroup), nameof(NewCarBrandSortOrder));
        private void RaiseCarModelProps() => RaiseAll(nameof(NewCarModelBrandId), nameof(NewCarModelName), nameof(NewCarModelYear), nameof(NewCarModelEngine), nameof(NewCarModelBasePrice));
        private void RaiseWarehouseProps() => RaiseAll(nameof(NewWarehouseName), nameof(NewWarehouseAddress), nameof(NewWarehouseIsMain));
        private void RaiseTransactionTypeProps() => RaiseAll(nameof(NewTransactionTypeName), nameof(NewTransactionCurrencyCode), nameof(NewTransactionCounterRate), nameof(NewTransactionIsActive));
        private void RaiseUsedCarProps() => RaiseAll(nameof(NewUsedCarName), nameof(NewUsedCarModelYear), nameof(NewUsedCarCarModelId), nameof(NewUsedCarPriceCurrency), nameof(NewUsedCarPriceBase), nameof(NewUsedCarPriceCounter), nameof(NewUsedCarLocation), nameof(NewUsedCarTransportation), nameof(NewUsedCarPartOut), nameof(NewUsedCarShipping), nameof(NewUsedCarCustoms));
        private void SetStatus(string message, bool isSuccess) => _statusCenter.SetStatus(message, isSuccess);

        private void RaiseAll(params string[] names) { foreach (var n in names) OnPropertyChanged(n); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
