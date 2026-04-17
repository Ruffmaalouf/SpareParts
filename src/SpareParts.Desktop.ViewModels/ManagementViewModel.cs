using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public LocationManagementViewModel LocationsFeature { get; } = new();
        public WarehouseManagementViewModel WarehousesFeature { get; } = new();
        public TransactionTypeManagementViewModel TransactionTypesFeature { get; } = new();
        public AccountingViewModel AccountingVm { get; }

        public UsersViewModel UsersVm { get; }
        public RolesViewModel RolesVm { get; }
        public ObservableCollection<CategoryDto> Categories { get; } = new();

        public ObservableCollection<CustomerDto> Customers => CustomersFeature.Customers;
        public ObservableCollection<SupplierDto> Suppliers => SuppliersFeature.Suppliers;
        public ObservableCollection<BrandDto> Brands => BrandsFeature.Brands;
        public ObservableCollection<PartDto> Parts => PartsFeature.Parts;
        public ObservableCollection<CarModelDto> CarModels => CarModelsFeature.CarModels;
        public ObservableCollection<CarBrandDto> CarBrands => CarModelsFeature.CarBrands;
        public ObservableCollection<LocationDto> Locations => LocationsFeature.Locations;
        public ObservableCollection<WarehouseDto> Warehouses => WarehousesFeature.Warehouses;
        public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = new();
        public ObservableCollection<CurrencyRateDisplayRow> CurrencyRateRows { get; } = new();
        public ObservableCollection<UsedCarModelOption> UsedCarModelOptions { get; } = new();
        public ObservableCollection<string> UsedCarCurrencyCodes { get; } = new();
        public ObservableCollection<string> LocationCurrencyCodes => UsedCarCurrencyCodes;
        public ObservableCollection<TransactionTypeDto> TransactionTypes => TransactionTypesFeature.TransactionTypes;
        public ObservableCollection<UsedCarEntry> UsedCars { get; } = new();
        public ObservableCollection<AccountingAccountRow> AccountingAccounts { get; } = new();
        public ObservableCollection<AccountingAccountRow> ConfiguredAccountingAccounts { get; } = new();
        public ObservableCollection<AccountingPostingRuleRow> AccountingPostingRules { get; } = new();


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
        public string UsedCarPriceLabel => $"Price ({NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _baseCurrencyCode})";
        public string UsedCarBasePriceLabel => $"Price Base ({BaseCurrencyCode})";
        public string UsedCarCounterPriceLabel => $"Price Counter ({CounterCurrencyCode})";
        public string UsedCarTransportationLabel => $"Transportation ({CounterCurrencyCode})";
        public string UsedCarPartOutLabel => $"Part-Out ({CounterCurrencyCode})";
        public string UsedCarShippingLabel => $"Shipping ({CounterCurrencyCode})";
        public string UsedCarCustomsLabel => $"Customs ({CounterCurrencyCode})";
        public string UsedCarTotalBeforeShippingLabel => $"Total Before Shipping ({CounterCurrencyCode})";
        public string UsedCarGrandTotalBaseLabel => $"Grand Total Base ({BaseCurrencyCode})";
        public string UsedCarGrandTotalCounterLabel => $"Grand Total Counter ({CounterCurrencyCode})";
        public string UsedCarsTotalBaseLabel => $"Total Base Amount ({BaseCurrencyCode})";
        public string UsedCarsTotalCounterLabel => $"Total Counter Amount ({CounterCurrencyCode})";
        public decimal UsedCarsTotalBaseAmount => decimal.Round(UsedCars.Sum(entry => entry.GrandTotalBase), 2, MidpointRounding.AwayFromZero);
        public decimal UsedCarsTotalCounterAmount => decimal.Round(UsedCars.Sum(entry => entry.GrandTotalCounter), 2, MidpointRounding.AwayFromZero);
        public int AccountingAccountCount => AccountingAccounts.Count;
        public int ActiveTransactionTypeCount => TransactionTypes.Count(item => item.IsActive);
        public decimal CustomerOpeningBalanceTotal => decimal.Round(Customers.Sum(customer => customer.OpeningBalance), 2, MidpointRounding.AwayFromZero);
        public decimal SupplierOpeningBalanceTotal => decimal.Round(Suppliers.Sum(supplier => supplier.OpeningBalance), 2, MidpointRounding.AwayFromZero);
        public string AccountingChartSummary => $"{AccountingAccountCount} seeded account(s) are available for the chart of accounts.";
        public string AccountingPostingSummary => $"{AccountingPostingRules.Count} posting flow(s) are currently wired in backend services.";
        public string AccountingOperationsSummary => $"{ActiveTransactionTypeCount} active transaction type(s) and {CurrencyRates.Count} currency rate snapshot(s) support the accounting workflow.";

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
        public CarBrandDto? SelectedCarBrand
        {
            get => CarModelsFeature.SelectedCarBrand;
            set
            {
                CarModelsFeature.SelectedCarBrand = value;
                OnPropertyChanged(nameof(SelectedCarBrand));
                if (value != null)
                {
                    CarModelsFeature.PopulateCarBrandForm(value);
                    RaiseCarBrandProps();
                }
            }
        }
        public CarModelDto? SelectedCarModel { get => CarModelsFeature.SelectedCarModel; set { CarModelsFeature.SelectedCarModel = value; OnPropertyChanged(nameof(SelectedCarModel)); if (value != null) { CarModelsFeature.PopulateForm(value); RaiseCarModelProps(); } } }
        public LocationDto? SelectedLocation
        {
            get => LocationsFeature.SelectedLocation;
            set
            {
                LocationsFeature.SelectedLocation = value;
                OnPropertyChanged(nameof(SelectedLocation));
                if (value != null)
                {
                    LocationsFeature.PopulateForm(value);
                    RaiseLocationProps();
                }
            }
        }
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
                if (value == null)
                {
                    return;
                }

                _newUsedCarCarModelId = value.CarModelId
                    ?? CarModels
                        .FirstOrDefault(m =>
                            string.Equals(m.Name, value.Car, StringComparison.OrdinalIgnoreCase))?.Id;
                _newUsedCarName = value.Car;
                _newUsedCarModelYear = value.ModelYear;
                _newUsedCarPriceCurrency = NormalizeCurrencyCode(value.PriceCurrency) ?? _baseCurrencyCode;
                _newUsedCarPrice = value.Price;
                _newUsedCarPriceBase = value.PriceBase;
                _newUsedCarPriceCounter = value.PriceCounter;
                _newUsedCarLocationId = value.LocationId
                    ?? Locations.FirstOrDefault(location =>
                        string.Equals(location.Name, value.Location, StringComparison.OrdinalIgnoreCase))?.LocationId;
                _newUsedCarTransportation = value.Transportation;
                _newUsedCarIsReceived = value.IsReceived;
                _newUsedCarIsShipped = value.IsShipped;
                _newUsedCarPartOut = value.PartOut;
                _newUsedCarShipping = value.Shipping;
                _newUsedCarCustoms = value.Customs;
                _newUsedCarTotalBeforeShipping = value.TotalBeforeShipping;
                _newUsedCarGrandTotalBase = value.GrandTotalBase;
                _newUsedCarGrandTotalCounter = value.GrandTotalCounter;

                RaiseUsedCarProps();
                OnPropertyChanged(nameof(UsedCarPriceLabel));
            }
        }

        public AccountingAccountRow? SelectedAccountingAccount
        {
            get => _selectedAccountingAccount;
            set
            {
                if (_selectedAccountingAccount == value) return;
                _selectedAccountingAccount = value;
                OnPropertyChanged(nameof(SelectedAccountingAccount));
                OnPropertyChanged(nameof(SelectedAccountingAccountTitle));
                OnPropertyChanged(nameof(SelectedAccountingAccountUsage));
                OnPropertyChanged(nameof(SelectedAccountingAccountRole));
                OnPropertyChanged(nameof(SelectedAccountingAccountParent));
            }
        }

        public string SelectedAccountingAccountTitle =>
            SelectedAccountingAccount == null
                ? "Select an account"
                : $"{SelectedAccountingAccount.Code} · {SelectedAccountingAccount.Name}";

        public string SelectedAccountingAccountUsage =>
            SelectedAccountingAccount?.UsageSummary
            ?? "Pick an account from the chart below to review how the current app uses it.";

        public string SelectedAccountingAccountRole => SelectedAccountingAccount?.PostingRoleLabel ?? "Reference";
        public string SelectedAccountingAccountParent => SelectedAccountingAccount?.ParentDisplay ?? "Root";

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
        public string NewPartAveragePrice { get => PartsFeature.NewPartAveragePrice; set { PartsFeature.NewPartAveragePrice = value; OnPropertyChanged(nameof(NewPartAveragePrice)); } }
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
        public string NewCarModelBodyType { get => CarModelsFeature.NewCarModelBodyType; set { CarModelsFeature.NewCarModelBodyType = value; OnPropertyChanged(nameof(NewCarModelBodyType)); } }
        public int NewCarModelBrandId { get => CarModelsFeature.NewCarModelBrandId; set { CarModelsFeature.NewCarModelBrandId = value; OnPropertyChanged(nameof(NewCarModelBrandId)); } }
        public string NewLocationName { get => LocationsFeature.NewLocationName; set { LocationsFeature.NewLocationName = value; OnPropertyChanged(nameof(NewLocationName)); } }
        public decimal NewLocationShippingFees { get => LocationsFeature.NewLocationShippingFees; set { LocationsFeature.NewLocationShippingFees = value; OnPropertyChanged(nameof(NewLocationShippingFees)); } }
        public string NewLocationShippingFeesCurrencyCode { get => LocationsFeature.NewLocationShippingFeesCurrencyCode; set { LocationsFeature.NewLocationShippingFeesCurrencyCode = NormalizeCurrencyCode(value) ?? _counterCurrencyCode; OnPropertyChanged(nameof(NewLocationShippingFeesCurrencyCode)); } }
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

        public decimal NewUsedCarPrice
        {
            get => _newUsedCarPrice;
            set
            {
                _newUsedCarPrice = value;
                OnPropertyChanged(nameof(NewUsedCarPrice));
                RecalculateUsedCarPriceConversions();
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
                OnPropertyChanged(nameof(UsedCarPriceLabel));
                RecalculateUsedCarPriceConversions();
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

        public int? NewUsedCarLocationId
        {
            get => _newUsedCarLocationId;
            set
            {
                _newUsedCarLocationId = value;
                OnPropertyChanged(nameof(NewUsedCarLocationId));
                SyncUsedCarTransportationFromSelectedLocation();
            }
        }

        public decimal NewUsedCarTransportation
        {
            get => _newUsedCarTransportation;
            set
            {
                _newUsedCarTransportation = value;
                OnPropertyChanged(nameof(NewUsedCarTransportation));
                RecalculateUsedCarTotals();
            }
        }

        public bool NewUsedCarIsReceived
        {
            get => _newUsedCarIsReceived;
            set
            {
                _newUsedCarIsReceived = value;
                OnPropertyChanged(nameof(NewUsedCarIsReceived));
            }
        }

        public bool NewUsedCarIsShipped
        {
            get => _newUsedCarIsShipped;
            set
            {
                _newUsedCarIsShipped = value;
                OnPropertyChanged(nameof(NewUsedCarIsShipped));
            }
        }

        public decimal NewUsedCarPartOut
        {
            get => _newUsedCarPartOut;
            set
            {
                _newUsedCarPartOut = value;
                OnPropertyChanged(nameof(NewUsedCarPartOut));
                RecalculateUsedCarTotals();
            }
        }

        public decimal NewUsedCarShipping
        {
            get => _newUsedCarShipping;
            set
            {
                _newUsedCarShipping = value;
                OnPropertyChanged(nameof(NewUsedCarShipping));
                RecalculateUsedCarTotals();
            }
        }

        public decimal NewUsedCarCustoms
        {
            get => _newUsedCarCustoms;
            set
            {
                _newUsedCarCustoms = value;
                OnPropertyChanged(nameof(NewUsedCarCustoms));
                RecalculateUsedCarTotals();
            }
        }

        public decimal NewUsedCarTotalBeforeShipping
        {
            get => _newUsedCarTotalBeforeShipping;
            set
            {
                _newUsedCarTotalBeforeShipping = value;
                OnPropertyChanged(nameof(NewUsedCarTotalBeforeShipping));
            }
        }

        public decimal NewUsedCarGrandTotalBase
        {
            get => _newUsedCarGrandTotalBase;
            set
            {
                _newUsedCarGrandTotalBase = value;
                OnPropertyChanged(nameof(NewUsedCarGrandTotalBase));
            }
        }

        public decimal NewUsedCarGrandTotalCounter
        {
            get => _newUsedCarGrandTotalCounter;
            set
            {
                _newUsedCarGrandTotalCounter = value;
                OnPropertyChanged(nameof(NewUsedCarGrandTotalCounter));
            }
        }

        public string Status => _statusCenter.Status;
        public ObservableCollection<StatusMessage> StatusMessages => _statusCenter.StatusMessages;
        public Brush StatusBrush => _statusCenter.StatusBrush;
        private bool _isLoading;
        private bool _isGeneratingPartNotes;
        private UsedCarEntry? _selectedUsedCar;
        private string _newUsedCarName = string.Empty;
        private int _newUsedCarModelYear;
        private int? _newUsedCarCarModelId;
        private string _newUsedCarPriceCurrency = "USD";
        private decimal _newUsedCarPrice;
        private decimal _newUsedCarPriceBase;
        private decimal _newUsedCarPriceCounter;
        private int? _newUsedCarLocationId;
        private decimal _newUsedCarTransportation;
        private bool _newUsedCarIsReceived;
        private bool _newUsedCarIsShipped;
        private decimal _newUsedCarPartOut;
        private decimal _newUsedCarShipping;
        private decimal _newUsedCarCustoms;
        private decimal _newUsedCarTotalBeforeShipping;
        private decimal _newUsedCarGrandTotalBase;
        private decimal _newUsedCarGrandTotalCounter;
        private AccountingAccountRow? _selectedAccountingAccount;
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

        public bool IsGeneratingPartNotes
        {
            get => _isGeneratingPartNotes;
            private set
            {
                if (_isGeneratingPartNotes == value) return;
                _isGeneratingPartNotes = value;
                OnPropertyChanged(nameof(IsGeneratingPartNotes));
                OnPropertyChanged(nameof(PartAiButtonText));
            }
        }

        public string PartAiButtonText => IsGeneratingPartNotes ? "AI is drafting notes..." : "✨ Draft Notes with AI";

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
        public ICommand DeleteCarBrandCommand { get; }
        public ICommand SaveCarModelCommand { get; }
        public ICommand DeleteCarModelCommand { get; }
        public ICommand SaveLocationCommand { get; }
        public ICommand DeleteLocationCommand { get; }
        public ICommand SaveWarehouseCommand { get; }
        public ICommand DeleteWarehouseCommand { get; }
        public ICommand SaveTransactionTypeCommand { get; }
        public ICommand DeleteTransactionTypeCommand { get; }
        public ICommand GeneratePartNotesCommand { get; }
        public ICommand StartNewManagementItemCommand { get; }
        public ICommand OpenUsedCarGalleryCommand { get; }
        public ICommand AddUsedCarCommand { get; }
        public ICommand RemoveUsedCarCommand { get; }

        public ManagementViewModel(
            ICrudApiClient crudApi,
            IAccountingApiClient accountingApi,
            ICarCatalogApiClient carCatalogApi,
            IPartsApiClient partsApi,
            UsersViewModel usersVm,
            RolesViewModel rolesVm,
            bool canViewSupplierTab = false,
            bool canEditSupplier = false,
            bool canModifySupplier = false,
            bool canDeleteSupplier = false)
        {
            UsersVm = usersVm;
            RolesVm = rolesVm;
            AccountingVm = new AccountingViewModel(accountingApi);
            SetSupplierPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);

            _coordinator = new ManagementCoordinator(
                crudApi,
                carCatalogApi,
                partsApi);
            UsedCars.CollectionChanged += UsedCars_CollectionChanged;

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
            DeleteCarBrandCommand = new RelayCommand(_ => _ = DeleteCarBrandAsync());
            SaveCarModelCommand = new RelayCommand(_ => _ = SaveCarModelAsync());
            DeleteCarModelCommand = new RelayCommand(_ => _ = DeleteCarModelAsync());
            SaveLocationCommand = new RelayCommand(_ => _ = SaveLocationAsync());
            DeleteLocationCommand = new RelayCommand(_ => _ = DeleteLocationAsync());
            SaveWarehouseCommand = new RelayCommand(_ => _ = SaveWarehouseAsync());
            DeleteWarehouseCommand = new RelayCommand(_ => _ = DeleteWarehouseAsync());
            SaveTransactionTypeCommand = new RelayCommand(_ => _ = SaveTransactionTypeAsync());
            DeleteTransactionTypeCommand = new RelayCommand(_ => _ = DeleteTransactionTypeAsync());
            GeneratePartNotesCommand = new RelayCommand(_ => _ = GeneratePartNotesAsync());
            StartNewManagementItemCommand = new RelayCommand(StartNewManagementItem);
            OpenUsedCarGalleryCommand = new RelayCommand(_ => OpenUsedCarGallery());
            AddUsedCarCommand = new RelayCommand(_ => _ = SaveUsedCarAsync());
            RemoveUsedCarCommand = new RelayCommand(_ => _ = RemoveSelectedUsedCarAsync());
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
                await AccountingVm.LoadSetupAsync();

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
                    Replace(Locations, loadResult.Locations);
                    Replace(Warehouses, loadResult.Warehouses);
                    Replace(CurrencyRates, loadResult.CurrencyRates);
                    ReplaceUsedCarCurrencyCodes();
                    EnsureLocationFormCurrencySelection();
                    SyncUsedCarTransportationFromSelectedLocation();
                    ReplaceCurrencyRateRows();
                    ReplaceUsedCars(loadResult.UsedCars);
                    Replace(TransactionTypes, loadResult.TransactionTypes);
                    RaiseAccountingDashboardProps();
                });

                SetStatus(
                    AccountingVm.IsStatusSuccess ? "✓ Data loaded." : AccountingVm.Status,
                    AccountingVm.IsStatusSuccess);
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

        private void InitializeAccountingCenter()
        {
            var accounts = new[]
            {
                new AccountingAccountRow
                {
                    Id = 1,
                    Code = "1000",
                    Name = "Cash",
                    AccountType = AccountType.Asset,
                    IsConfiguredPostingAccount = true,
                    PostingRoleLabel = "Sales cash account",
                    UsageSummary = "Receives the debit side of sales postings and represents immediate collections."
                },
                new AccountingAccountRow
                {
                    Id = 2,
                    Code = "1100",
                    Name = "Inventory",
                    AccountType = AccountType.Asset,
                    IsConfiguredPostingAccount = true,
                    PostingRoleLabel = "Inventory control",
                    UsageSummary = "Credited during sales and debited during purchases to keep stock valuation aligned with movement."
                },
                new AccountingAccountRow
                {
                    Id = 3,
                    Code = "2000",
                    Name = "Accounts Payable",
                    AccountType = AccountType.Liability,
                    PostingRoleLabel = "Reference liability",
                    UsageSummary = "Seeded as the liability bucket for supplier balances and future credit-purchase workflows."
                },
                new AccountingAccountRow
                {
                    Id = 4,
                    Code = "3000",
                    Name = "Owner Equity",
                    AccountType = AccountType.Equity,
                    IsConfiguredPostingAccount = true,
                    PostingRoleLabel = "Purchase offset",
                    UsageSummary = "Currently configured as the offset account for purchase postings in the backend accounting options."
                },
                new AccountingAccountRow
                {
                    Id = 5,
                    Code = "4000",
                    Name = "Sales Revenue",
                    AccountType = AccountType.Income,
                    IsConfiguredPostingAccount = true,
                    PostingRoleLabel = "Sales revenue",
                    UsageSummary = "Credited whenever a sales invoice is posted."
                },
                new AccountingAccountRow
                {
                    Id = 6,
                    Code = "5000",
                    Name = "Cost of Goods Sold",
                    AccountType = AccountType.Expense,
                    IsConfiguredPostingAccount = true,
                    PostingRoleLabel = "COGS expense",
                    UsageSummary = "Debited alongside each sale to recognize the cost of inventory leaving stock."
                },
                new AccountingAccountRow
                {
                    Id = 7,
                    Code = "6000",
                    Name = "Operating Expenses",
                    AccountType = AccountType.Expense,
                    PostingRoleLabel = "Reference expense",
                    UsageSummary = "Available for future non-inventory expense postings once more accounting flows are added."
                }
            };

            var postingRules = new[]
            {
                new AccountingPostingRuleRow
                {
                    Area = "Sales",
                    Trigger = "Create or update a sales invoice",
                    DebitAccounts = "1000 Cash, 5000 Cost of Goods Sold",
                    CreditAccounts = "4000 Sales Revenue, 1100 Inventory",
                    Notes = "Matches the current sales accounting strategy used by the backend service."
                },
                new AccountingPostingRuleRow
                {
                    Area = "Purchases",
                    Trigger = "Create a purchase invoice",
                    DebitAccounts = "1100 Inventory",
                    CreditAccounts = "3000 Owner Equity",
                    Notes = "Reflects the current purchase offset account configured in application settings."
                }
            };

            Replace(AccountingAccounts, accounts);
            Replace(ConfiguredAccountingAccounts, accounts.Where(account => account.IsConfiguredPostingAccount));
            Replace(AccountingPostingRules, postingRules);
            SelectedAccountingAccount = ConfiguredAccountingAccounts.FirstOrDefault() ?? AccountingAccounts.FirstOrDefault();

            RaiseAccountingDashboardProps();
            OnPropertyChanged(nameof(AccountingChartSummary));
            OnPropertyChanged(nameof(AccountingPostingSummary));
        }

        private void RaiseAccountingDashboardProps()
        {
            OnPropertyChanged(nameof(AccountingAccountCount));
            OnPropertyChanged(nameof(ActiveTransactionTypeCount));
            OnPropertyChanged(nameof(CustomerOpeningBalanceTotal));
            OnPropertyChanged(nameof(SupplierOpeningBalanceTotal));
            OnPropertyChanged(nameof(AccountingChartSummary));
            OnPropertyChanged(nameof(AccountingPostingSummary));
            OnPropertyChanged(nameof(AccountingOperationsSummary));
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
            OnPropertyChanged(nameof(UsedCarPriceLabel));
            OnPropertyChanged(nameof(UsedCarBasePriceLabel));
            OnPropertyChanged(nameof(UsedCarCounterPriceLabel));
            OnPropertyChanged(nameof(UsedCarTransportationLabel));
            OnPropertyChanged(nameof(UsedCarPartOutLabel));
            OnPropertyChanged(nameof(UsedCarShippingLabel));
            OnPropertyChanged(nameof(UsedCarCustomsLabel));
            OnPropertyChanged(nameof(UsedCarTotalBeforeShippingLabel));
            OnPropertyChanged(nameof(UsedCarGrandTotalBaseLabel));
            OnPropertyChanged(nameof(UsedCarGrandTotalCounterLabel));
            OnPropertyChanged(nameof(UsedCarsTotalBaseLabel));
            OnPropertyChanged(nameof(UsedCarsTotalCounterLabel));
            EnsureLocationFormCurrencySelection();
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
                .OrderBy(model => model.CarBrandName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .Select(model => new UsedCarModelOption
                {
                    Id = model.Id,
                    CarBrandName = model.CarBrandName,
                    Name = model.Name,
                    BodyType = model.BodyType
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

        private void ReplaceUsedCars(IEnumerable<UsedCarDto> usedCars)
        {
            var selectedUsedCarId = SelectedUsedCar?.Id;
            var rows = usedCars
                .Select(MapUsedCar)
                .ToList();

            Replace(UsedCars, rows);

            if (selectedUsedCarId.HasValue)
            {
                SelectedUsedCar = UsedCars.FirstOrDefault(entry => entry.Id == selectedUsedCarId.Value);
            }

            RaiseUsedCarSummaryProps();
        }

        private void UsedCars_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var row in e.OldItems.OfType<UsedCarEntry>())
                {
                    row.PropertyChanged -= UsedCarEntryOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var row in e.NewItems.OfType<UsedCarEntry>())
                {
                    row.PropertyChanged += UsedCarEntryOnPropertyChanged;
                }
            }

            RaiseUsedCarSummaryProps();
        }

        private void UsedCarEntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(UsedCarEntry.GrandTotalBase) or nameof(UsedCarEntry.GrandTotalCounter))
            {
                RaiseUsedCarSummaryProps();
            }
        }

        private static UsedCarEntry MapUsedCar(UsedCarDto usedCar)
        {
            return new UsedCarEntry
            {
                Id = usedCar.Id,
                CarModelId = usedCar.CarModelId,
                LocationId = usedCar.LocationId,
                Car = usedCar.Car,
                ModelYear = usedCar.ModelYear,
                PriceCurrency = usedCar.PriceCurrency,
                Price = usedCar.Price,
                PriceBase = usedCar.PriceBase,
                PriceCounter = usedCar.PriceCounter,
                Location = usedCar.Location,
                Transportation = usedCar.Transportation,
                IsReceived = usedCar.IsReceived,
                IsShipped = usedCar.IsShipped,
                PartOut = usedCar.PartOut,
                Shipping = usedCar.Shipping,
                Customs = usedCar.Customs,
                TotalBeforeShipping = usedCar.TotalBeforeShipping,
                GrandTotalBase = usedCar.GrandTotalBase,
                GrandTotalCounter = usedCar.GrandTotalCounter
            };
        }

        private void EnsureLocationFormCurrencySelection()
        {
            if (NormalizeCurrencyCode(LocationsFeature.NewLocationShippingFeesCurrencyCode) != null)
            {
                return;
            }

            LocationsFeature.NewLocationShippingFeesCurrencyCode = _counterCurrencyCode;
            OnPropertyChanged(nameof(NewLocationShippingFeesCurrencyCode));
        }

        private void SyncUsedCarTransportationFromSelectedLocation()
        {
            if (NewUsedCarLocationId is not int locationId)
            {
                NewUsedCarTransportation = 0m;
                return;
            }

            var selectedLocation = Locations.FirstOrDefault(location => location.LocationId == locationId);
            if (selectedLocation == null)
            {
                NewUsedCarTransportation = 0m;
                return;
            }

            var locationCurrencyCode = NormalizeCurrencyCode(selectedLocation.ShippingFeesCurrencyCode) ?? _counterCurrencyCode;
            var rateToCounterCurrency = ResolveRateToCounterCurrency(locationCurrencyCode);
            var convertedTransportation = rateToCounterCurrency > 0
                ? decimal.Round(selectedLocation.ShippingFees * rateToCounterCurrency, 2, MidpointRounding.AwayFromZero)
                : 0m;

            NewUsedCarTransportation = convertedTransportation;
        }

        private void RecalculateUsedCarPriceConversions()
        {
            var selectedCurrencyCode = NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _baseCurrencyCode;
            var selectedToBaseRate = ResolveRateToBaseCurrency(selectedCurrencyCode);
            if (selectedToBaseRate <= 0)
            {
                _newUsedCarPriceBase = 0m;
                _newUsedCarPriceCounter = 0m;
                OnPropertyChanged(nameof(NewUsedCarPriceBase));
                OnPropertyChanged(nameof(NewUsedCarPriceCounter));
                RecalculateUsedCarTotals();
                return;
            }

            var counterToBaseRate = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterToBaseRate <= 0)
            {
                counterToBaseRate = _defaultCounterRate > 0 ? _defaultCounterRate : 1m;
            }

            var normalizedPrice = Math.Max(NewUsedCarPrice, 0m);
            var convertedBasePrice = decimal.Round(normalizedPrice * selectedToBaseRate, 2, MidpointRounding.AwayFromZero);
            var convertedCounterPrice = counterToBaseRate > 0
                ? decimal.Round(convertedBasePrice / counterToBaseRate, 2, MidpointRounding.AwayFromZero)
                : convertedBasePrice;

            _newUsedCarPriceBase = convertedBasePrice;
            _newUsedCarPriceCounter = convertedCounterPrice;
            OnPropertyChanged(nameof(NewUsedCarPriceBase));
            OnPropertyChanged(nameof(NewUsedCarPriceCounter));
            RecalculateUsedCarTotals();
        }

        private void RecalculateUsedCarTotals()
        {
            var counterExpensesTotal = Math.Max(NewUsedCarTransportation, 0m)
                + Math.Max(NewUsedCarPartOut, 0m)
                + Math.Max(NewUsedCarShipping, 0m)
                + Math.Max(NewUsedCarCustoms, 0m);
            var counterToBaseRate = ResolveRateToBaseCurrency(_counterCurrencyCode);
            if (counterToBaseRate <= 0)
            {
                counterToBaseRate = _defaultCounterRate > 0 ? _defaultCounterRate : 1m;
            }

            _newUsedCarTotalBeforeShipping = decimal.Round(
                NewUsedCarPriceCounter + Math.Max(NewUsedCarTransportation, 0m),
                2,
                MidpointRounding.AwayFromZero);
            _newUsedCarGrandTotalCounter = decimal.Round(
                NewUsedCarPriceCounter + counterExpensesTotal,
                2,
                MidpointRounding.AwayFromZero);
            _newUsedCarGrandTotalBase = decimal.Round(
                NewUsedCarPriceBase + (counterExpensesTotal * counterToBaseRate),
                2,
                MidpointRounding.AwayFromZero);

            OnPropertyChanged(nameof(NewUsedCarTotalBeforeShipping));
            OnPropertyChanged(nameof(NewUsedCarGrandTotalBase));
            OnPropertyChanged(nameof(NewUsedCarGrandTotalCounter));
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

        private async Task GeneratePartNotesAsync()
        {
            if (IsGeneratingPartNotes)
            {
                return;
            }

            IsGeneratingPartNotes = true;
            SetStatus("Drafting part notes with AI…", true);

            try
            {
                var categoryLookup = Categories.ToDictionary(item => item.Id, item => item.Name);
                var brandLookup = Brands.ToDictionary(item => item.Id, item => item.Name);

                var result = await _coordinator.GeneratePartNotesAsync(PartsFeature, categoryLookup, brandLookup);
                SetStatus(result.Message, result.Success);
                if (result.Success)
                {
                    OnPropertyChanged(nameof(NewPartNotes));
                    OnPropertyChanged(nameof(NewPartAveragePrice));
                }
            }
            finally
            {
                IsGeneratingPartNotes = false;
            }
        }

        private async Task SaveCarBrandAsync()
        {
            var result = await _coordinator.SaveCarBrandAsync(CarModelsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CarModelsFeature.ClearCarBrandForm();
            RaiseAll(
                nameof(NewCarBrandName),
                nameof(NewCarBrandCountry),
                nameof(NewCarBrandRegionGroup),
                nameof(NewCarBrandSortOrder),
                nameof(SelectedCarBrand));
        }

        private async Task DeleteCarBrandAsync()
        {
            var result = await _coordinator.DeleteCarBrandAsync(SelectedCarBrand);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            CarModelsFeature.ClearCarBrandForm();
            OnPropertyChanged(nameof(SelectedCarBrand));
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

        private async Task SaveLocationAsync()
        {
            var result = await _coordinator.SaveLocationAsync(LocationsFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            LocationsFeature.ClearForm(_counterCurrencyCode);
            RaiseLocationProps();
            OnPropertyChanged(nameof(SelectedLocation));
        }

        private async Task DeleteLocationAsync()
        {
            var result = await _coordinator.DeleteLocationAsync(SelectedLocation);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            LocationsFeature.ClearForm(_counterCurrencyCode);
            OnPropertyChanged(nameof(SelectedLocation));
            RaiseLocationProps();
        }

        private async Task SaveWarehouseAsync()
        {
            var result = await _coordinator.SaveWarehouseAsync(WarehousesFeature);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            WarehousesFeature.ClearForm();
            RaiseWarehouseProps();
            OnPropertyChanged(nameof(SelectedWarehouse));
        }

        private async Task DeleteWarehouseAsync()
        {
            var result = await _coordinator.DeleteWarehouseAsync(SelectedWarehouse);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            WarehousesFeature.ClearForm();
            OnPropertyChanged(nameof(SelectedWarehouse));
            RaiseWarehouseProps();
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

        private void StartNewManagementItem(object? parameter)
        {
            switch (parameter as string)
            {
                case "Customer":
                    CustomersFeature.ClearForm();
                    OnPropertyChanged(nameof(SelectedCustomer));
                    RaiseCustomerProps();
                    break;
                case "Supplier":
                    SuppliersFeature.ClearForm();
                    _supplierPermissions.IsEditingSupplier = false;
                    OnPropertyChanged(nameof(SelectedSupplier));
                    OnPropertyChanged(nameof(CanSaveSupplier));
                    RaiseSupplierProps();
                    break;
                case "Brand":
                    BrandsFeature.ClearForm();
                    OnPropertyChanged(nameof(SelectedBrand));
                    RaiseAll(nameof(NewBrandName), nameof(NewBrandIsActive));
                    break;
                case "Part":
                    PartsFeature.ClearForm(_baseCurrencyCode);
                    OnPropertyChanged(nameof(SelectedPart));
                    RaisePartProps();
                    break;
                case "CarBrand":
                    CarModelsFeature.ClearCarBrandForm();
                    OnPropertyChanged(nameof(SelectedCarBrand));
                    RaiseCarBrandProps();
                    break;
                case "CarModel":
                    CarModelsFeature.ClearForm();
                    OnPropertyChanged(nameof(SelectedCarModel));
                    RaiseCarModelProps();
                    break;
                case "Location":
                    LocationsFeature.ClearForm(_counterCurrencyCode);
                    OnPropertyChanged(nameof(SelectedLocation));
                    RaiseLocationProps();
                    break;
                case "Warehouse":
                    WarehousesFeature.ClearForm();
                    OnPropertyChanged(nameof(SelectedWarehouse));
                    RaiseWarehouseProps();
                    break;
                case "TransactionType":
                    TransactionTypesFeature.ClearForm(_counterCurrencyCode);
                    OnPropertyChanged(nameof(SelectedTransactionType));
                    RaiseTransactionTypeProps();
                    break;
                case "UsedCar":
                    SelectedUsedCar = null;
                    ClearUsedCarForm();
                    break;
            }
        }

        private async Task SaveUsedCarAsync()
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

            if (NewUsedCarLocationId is not int locationId)
            {
                SetStatus("✗ Select a location from the Locations tab list first.", false);
                return;
            }

            if (NewUsedCarIsReceived && NewUsedCarCustoms <= 0)
            {
                SetStatus("✗ Customs should be different than 0 when the car is marked as received.", false);
                return;
            }

            var request = new CreateUsedCarRequest
            {
                CarModelId = carModelId,
                ModelYear = NewUsedCarModelYear,
                PriceCurrency = NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _baseCurrencyCode,
                Price = NewUsedCarPrice,
                LocationId = locationId,
                IsReceived = NewUsedCarIsReceived,
                IsShipped = NewUsedCarIsShipped,
                PartOut = NewUsedCarPartOut,
                Shipping = NewUsedCarShipping,
                Customs = NewUsedCarCustoms
            };

            var result = await _coordinator.SaveUsedCarAsync(request, SelectedUsedCar);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SelectedUsedCar = null;
            ClearUsedCarForm();
        }

        private async Task RemoveSelectedUsedCarAsync()
        {
            var result = await _coordinator.DeleteUsedCarAsync(SelectedUsedCar);
            SetStatus(result.Message, result.Success);
            if (!result.Success) return;

            await LoadAllAsync();
            SelectedUsedCar = null;
            ClearUsedCarForm();
        }

        private void OpenUsedCarGallery()
        {
            if (SelectedUsedCar is not { Id: > 0 } usedCar)
            {
                CustomMessageBox.Show("Select a used car row first, then open the gallery.", "Gallery", "Warning");
                return;
            }

            var galleryWindow = new UsedCarGalleryWindow(_coordinator, usedCar);
            var owner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive);

            if (owner != null && owner != galleryWindow)
            {
                galleryWindow.Owner = owner;
                galleryWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }

            galleryWindow.ShowDialog();
        }

        private void ClearUsedCarForm()
        {
            NewUsedCarName = string.Empty;
            NewUsedCarModelYear = 0;
            NewUsedCarCarModelId = null;
            NewUsedCarPriceCurrency = _baseCurrencyCode;
            NewUsedCarPrice = 0;
            NewUsedCarPriceBase = 0;
            NewUsedCarPriceCounter = 0;
            NewUsedCarLocationId = null;
            NewUsedCarTransportation = 0;
            NewUsedCarIsReceived = false;
            NewUsedCarIsShipped = false;
            NewUsedCarPartOut = 0;
            NewUsedCarShipping = 0;
            NewUsedCarCustoms = 0;
            NewUsedCarTotalBeforeShipping = 0;
            NewUsedCarGrandTotalBase = 0;
            NewUsedCarGrandTotalCounter = 0;
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

            var option = UsedCarModelOptions.FirstOrDefault(item => item.Id == carModelId);
            NewUsedCarName = option?.DisplayName ?? selectedModel.Name;
        }

        private void RaiseCustomerProps() => RaiseAll(nameof(NewCustomerName), nameof(NewCustomerPhone), nameof(NewCustomerEmail), nameof(NewCustomerAddress), nameof(NewCustomerTax), nameof(NewCustomerBalance));
        private void RaiseSupplierProps() => RaiseAll(nameof(NewSupplierName), nameof(NewSupplierPhone), nameof(NewSupplierEmail), nameof(NewSupplierAddress), nameof(NewSupplierTax), nameof(NewSupplierBalance));
        private void RaisePartProps() => RaiseAll(nameof(NewPartCode), nameof(NewPartName), nameof(NewPartOEM), nameof(NewPartCategoryId), nameof(NewPartBrandId), nameof(NewPartCostPrice), nameof(NewPartSalePrice), nameof(NewPartAveragePrice), nameof(NewPartCurrency), nameof(NewPartMinStock), nameof(NewPartNotes));
        private void RaiseCarBrandProps() => RaiseAll(nameof(NewCarBrandName), nameof(NewCarBrandCountry), nameof(NewCarBrandRegionGroup), nameof(NewCarBrandSortOrder));
        private void RaiseCarModelProps() => RaiseAll(nameof(NewCarModelBrandId), nameof(NewCarModelName), nameof(NewCarModelBodyType));
        private void RaiseLocationProps() => RaiseAll(nameof(NewLocationName), nameof(NewLocationShippingFees), nameof(NewLocationShippingFeesCurrencyCode));
        private void RaiseWarehouseProps() => RaiseAll(nameof(NewWarehouseName), nameof(NewWarehouseAddress), nameof(NewWarehouseIsMain));
        private void RaiseTransactionTypeProps() => RaiseAll(nameof(NewTransactionTypeName), nameof(NewTransactionCurrencyCode), nameof(NewTransactionCounterRate), nameof(NewTransactionIsActive));
        private void RaiseUsedCarProps() => RaiseAll(
            nameof(NewUsedCarName),
            nameof(NewUsedCarModelYear),
            nameof(NewUsedCarCarModelId),
            nameof(NewUsedCarPriceCurrency),
            nameof(NewUsedCarPrice),
            nameof(NewUsedCarPriceBase),
            nameof(NewUsedCarPriceCounter),
            nameof(NewUsedCarLocationId),
            nameof(NewUsedCarTransportation),
            nameof(NewUsedCarIsReceived),
            nameof(NewUsedCarIsShipped),
            nameof(NewUsedCarPartOut),
            nameof(NewUsedCarShipping),
            nameof(NewUsedCarCustoms),
            nameof(NewUsedCarTotalBeforeShipping),
            nameof(NewUsedCarGrandTotalBase),
            nameof(NewUsedCarGrandTotalCounter),
            nameof(UsedCarPriceLabel),
            nameof(UsedCarPartOutLabel));
        private void RaiseUsedCarSummaryProps() => RaiseAll(
            nameof(UsedCarsTotalBaseAmount),
            nameof(UsedCarsTotalCounterAmount));
        private void SetStatus(string message, bool isSuccess) => _statusCenter.SetStatus(message, isSuccess);

        private void RaiseAll(params string[] names) { foreach (var n in names) OnPropertyChanged(n); }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
