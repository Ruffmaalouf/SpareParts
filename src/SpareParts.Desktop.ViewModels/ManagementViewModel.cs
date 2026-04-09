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
        private decimal _defaultCounterRate = 1m;

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
        public ObservableCollection<CurrencyRateDisplayRow> CurrencyRateRows { get; } = new();
        public ObservableCollection<UsedCarModelOption> UsedCarModelOptions { get; } = new();
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
        public string BaseCurrencyCode => _baseCurrencyCode;
        public string CounterCurrencyCode => _counterCurrencyCode;
        public string CurrencyRatesSummary => $"Base {BaseCurrencyCode} | Counter {CounterCurrencyCode}";
        public string UsedCarBasePriceLabel => $"Price Base ({BaseCurrencyCode})";
        public string UsedCarCounterPriceLabel => $"Price Counter ({CounterCurrencyCode})";

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
                    NewUsedCarCarModelId = value.CarModelId
                        ?? CarModels
                            .FirstOrDefault(m =>
                                string.Equals(m.Name, value.Car, StringComparison.OrdinalIgnoreCase)
                                && int.TryParse(m.Year, out var modelYear)
                                && modelYear == value.ModelYear)?.Id;
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
                var normalized = NormalizeCurrencyCode(value) ?? _baseCurrencyCode;
                if (string.Equals(_newUsedCarPriceCurrency, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _newUsedCarPriceCurrency = normalized;
                OnPropertyChanged(nameof(NewUsedCarPriceCurrency));
                RecalculateUsedCarPricesFromCurrencyChange();
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
                    ReplaceUsedCarModelOptions();
                    Replace(Warehouses, loadResult.Warehouses);
                    Replace(CurrencyRates, loadResult.CurrencyRates);
                    ReplaceUsedCarCurrencyCodes();
                    ReplaceCurrencyRateRows();
                    SyncUsedCarsFromCarModels();
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
            if (byKey.TryGetValue("DefaultCounterRate", out var defaultCounterRate)
                && decimal.TryParse(defaultCounterRate, out var parsedRate)
                && parsedRate > 0)
            {
                _defaultCounterRate = parsedRate;
            }

            OnPropertyChanged(nameof(BaseCurrencyCode));
            OnPropertyChanged(nameof(CounterCurrencyCode));
            OnPropertyChanged(nameof(CurrencyRatesSummary));
            OnPropertyChanged(nameof(UsedCarBasePriceLabel));
            OnPropertyChanged(nameof(UsedCarCounterPriceLabel));
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
                .Select(rate => NormalizeCurrencyCode(rate.Code))
                .OfType<string>()
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

            Replace(UsedCarCurrencyCodes, codes);
        }

        private void ReplaceUsedCarModelOptions()
        {
            var options = CarModels
                .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.Year, StringComparer.OrdinalIgnoreCase)
                .Select(model => new UsedCarModelOption
                {
                    Id = model.Id,
                    Name = model.Name,
                    Year = model.Year,
                    BasePrice = model.BasePrice
                });

            Replace(UsedCarModelOptions, options);
        }

        private void ReplaceCurrencyRateRows()
        {
            var codes = CurrencyRates
                .Select(rate => NormalizeCurrencyCode(rate.Code))
                .OfType<string>()
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

            var rows = codes.Select(code => new CurrencyRateDisplayRow
            {
                Code = code,
                BaseRate = decimal.Round(ResolveRateToBaseCurrency(code), 6, MidpointRounding.AwayFromZero),
                CounterRate = decimal.Round(ResolveRateToCounterCurrency(code), 6, MidpointRounding.AwayFromZero),
                SnapshotUtc = CurrencyRates.FirstOrDefault(rate =>
                    string.Equals(NormalizeCurrencyCode(rate.Code), code, StringComparison.OrdinalIgnoreCase))?.SnapshotUtc
            });

            Replace(CurrencyRateRows, rows);
        }

        private void SyncUsedCarsFromCarModels()
        {
            var existingRowsByCarModelId = UsedCars
                .Where(entry => entry.CarModelId.HasValue)
                .GroupBy(entry => entry.CarModelId!.Value)
                .ToDictionary(group => group.Key, group => group.First());

            var selectedCarModelId = SelectedUsedCar?.CarModelId;
            var syncedRows = CarModels
                .OrderBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.Year, StringComparer.OrdinalIgnoreCase)
                .Select(model =>
                {
                    existingRowsByCarModelId.TryGetValue(model.Id, out var existingRow);
                    return CreateUsedCarEntry(model, existingRow);
                })
                .ToList();

            Replace(UsedCars, syncedRows);

            if (selectedCarModelId.HasValue)
            {
                SelectedUsedCar = UsedCars.FirstOrDefault(entry => entry.CarModelId == selectedCarModelId.Value);
            }
        }

        private UsedCarEntry CreateUsedCarEntry(CarModelDto model, UsedCarEntry? existingRow)
        {
            var basePrice = existingRow is { PriceBase: > 0 }
                ? existingRow.PriceBase
                : decimal.Round(model.BasePrice, 2, MidpointRounding.AwayFromZero);

            return new UsedCarEntry
            {
                CarModelId = model.Id,
                Car = model.Name,
                ModelYear = int.TryParse(model.Year, out var modelYear) ? modelYear : 0,
                PriceCurrency = NormalizeCurrencyCode(existingRow?.PriceCurrency) ?? _baseCurrencyCode,
                PriceBase = basePrice,
                PriceCounter = existingRow is { PriceCounter: > 0 }
                    ? existingRow.PriceCounter
                    : ConvertBasePriceToCounterPrice(basePrice),
                Location = existingRow?.Location ?? string.Empty,
                Transportation = existingRow?.Transportation ?? 0m,
                PartOut = existingRow?.PartOut ?? string.Empty,
                Shipping = existingRow?.Shipping ?? 0m,
                Customs = existingRow?.Customs ?? 0m
            };
        }

        private void RecalculateUsedCarPricesFromCurrencyChange()
        {
            var selectedCurrencyCode = NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _baseCurrencyCode;
            var amountInSelectedCurrency = ResolveAmountInSelectedCurrency(selectedCurrencyCode);
            var selectedToBaseRate = ResolveRateToBaseCurrency(selectedCurrencyCode);
            if (selectedToBaseRate <= 0)
            {
                return;
            }

            var counterToBaseRate = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterToBaseRate <= 0)
            {
                counterToBaseRate = 1m;
            }

            var convertedBasePrice = decimal.Round(amountInSelectedCurrency * selectedToBaseRate, 2, MidpointRounding.AwayFromZero);
            var convertedCounterPrice = decimal.Round(convertedBasePrice / counterToBaseRate, 2, MidpointRounding.AwayFromZero);

            _newUsedCarPriceBase = convertedBasePrice;
            _newUsedCarPriceCounter = convertedCounterPrice;
            OnPropertyChanged(nameof(NewUsedCarPriceBase));
            OnPropertyChanged(nameof(NewUsedCarPriceCounter));
        }

        private decimal ResolveAmountInSelectedCurrency(string selectedCurrencyCode)
        {
            if (string.Equals(selectedCurrencyCode, _baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return NewUsedCarPriceBase;
            }

            if (string.Equals(selectedCurrencyCode, _counterCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return NewUsedCarPriceCounter;
            }

            if (NewUsedCarPriceBase > 0)
            {
                var selectedToBaseRate = ResolveRateToBaseCurrency(selectedCurrencyCode);
                if (selectedToBaseRate > 0)
                {
                    return NewUsedCarPriceBase / selectedToBaseRate;
                }
            }

            if (NewUsedCarPriceCounter > 0)
            {
                var counterToBaseRate = ResolveRateToBaseCurrency(_counterCurrencyCode);
                var selectedToBaseRate = ResolveRateToBaseCurrency(selectedCurrencyCode);
                if (counterToBaseRate > 0 && selectedToBaseRate > 0)
                {
                    var baseAmount = NewUsedCarPriceCounter * counterToBaseRate;
                    return baseAmount / selectedToBaseRate;
                }
            }

            return 0m;
        }

        private decimal ResolveRateToBaseCurrency(string? currencyCode)
        {
            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode == null)
            {
                return 0m;
            }

            if (string.Equals(normalizedCurrencyCode, _baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            var currency = CurrencyRates.FirstOrDefault(rate =>
                string.Equals(NormalizeCurrencyCode(rate.Code), normalizedCurrencyCode, StringComparison.OrdinalIgnoreCase));
            if (currency == null || currency.RateToUsd <= 0)
            {
                return string.Equals(normalizedCurrencyCode, _counterCurrencyCode, StringComparison.OrdinalIgnoreCase)
                    ? _defaultCounterRate
                    : 0m;
            }

            var normalizedBaseCode = NormalizeCurrencyCode(currency.BaseCode) ?? _baseCurrencyCode;
            if (string.Equals(normalizedCurrencyCode, normalizedBaseCode, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            return 1m / currency.RateToUsd;
        }

        private decimal ResolveRateToCounterCurrency(string? currencyCode)
        {
            var rateToBaseCurrency = ResolveRateToBaseCurrency(currencyCode);
            if (rateToBaseCurrency <= 0)
            {
                return 0m;
            }

            var counterRateToBaseCurrency = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterRateToBaseCurrency <= 0)
            {
                counterRateToBaseCurrency = _defaultCounterRate;
            }

            if (counterRateToBaseCurrency <= 0)
            {
                return 0m;
            }

            return rateToBaseCurrency / counterRateToBaseCurrency;
        }

        private decimal ConvertBasePriceToCounterPrice(decimal basePrice)
        {
            var counterRateToBaseCurrency = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterRateToBaseCurrency <= 0)
            {
                counterRateToBaseCurrency = _defaultCounterRate;
            }

            return counterRateToBaseCurrency > 0
                ? decimal.Round(basePrice / counterRateToBaseCurrency, 2, MidpointRounding.AwayFromZero)
                : decimal.Round(basePrice, 2, MidpointRounding.AwayFromZero);
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return null;
            }

            var normalized = currencyCode.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : null;
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

        private Task SaveWarehouseAsync()
        {
            SetStatus("✗ Warehouse save flow is not wired yet.", false);
            return Task.CompletedTask;
        }

        private Task DeleteWarehouseAsync()
        {
            SetStatus("✗ Warehouse delete flow is not wired yet.", false);
            return Task.CompletedTask;
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
            if (NewUsedCarCarModelId is not int carModelId)
            {
                SetStatus("✗ Select a car model from the Cars tab list first.", false);
                return;
            }

            var selectedModel = CarModels.FirstOrDefault(model => model.Id == carModelId);
            if (selectedModel == null)
            {
                SetStatus("✗ The selected car model no longer exists.", false);
                return;
            }

            var targetEntry = SelectedUsedCar?.CarModelId == carModelId
                ? SelectedUsedCar
                : UsedCars.FirstOrDefault(entry => entry.CarModelId == carModelId);

            if (targetEntry == null)
            {
                targetEntry = CreateUsedCarEntry(selectedModel, null);
                UsedCars.Add(targetEntry);
            }

            SelectedUsedCar = targetEntry;

            targetEntry.CarModelId = carModelId;
            targetEntry.Car = selectedModel.Name;
            targetEntry.ModelYear = NewUsedCarModelYear;
            targetEntry.PriceCurrency = string.IsNullOrWhiteSpace(NewUsedCarPriceCurrency) ? _baseCurrencyCode : NewUsedCarPriceCurrency.Trim().ToUpperInvariant();
            targetEntry.PriceBase = NewUsedCarPriceBase;
            targetEntry.PriceCounter = NewUsedCarPriceCounter;
            targetEntry.Location = NewUsedCarLocation.Trim();
            targetEntry.Transportation = NewUsedCarTransportation;
            targetEntry.PartOut = NewUsedCarPartOut.Trim();
            targetEntry.Shipping = NewUsedCarShipping;
            targetEntry.Customs = NewUsedCarCustoms;

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

            if (selectedModel.BasePrice > 0)
            {
                ApplyUsedCarPricesFromBasePrice(selectedModel.BasePrice);
            }
        }

        private void ApplyUsedCarPricesFromBasePrice(decimal basePrice)
        {
            var normalizedBasePrice = decimal.Round(basePrice, 2, MidpointRounding.AwayFromZero);
            var counterRateToBaseCurrency = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterRateToBaseCurrency <= 0)
            {
                counterRateToBaseCurrency = _defaultCounterRate;
            }

            _newUsedCarPriceCurrency = _baseCurrencyCode;
            _newUsedCarPriceBase = normalizedBasePrice;
            _newUsedCarPriceCounter = counterRateToBaseCurrency > 0
                ? decimal.Round(normalizedBasePrice / counterRateToBaseCurrency, 2, MidpointRounding.AwayFromZero)
                : normalizedBasePrice;

            OnPropertyChanged(nameof(NewUsedCarPriceCurrency));
            OnPropertyChanged(nameof(NewUsedCarPriceBase));
            OnPropertyChanged(nameof(NewUsedCarPriceCounter));
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
