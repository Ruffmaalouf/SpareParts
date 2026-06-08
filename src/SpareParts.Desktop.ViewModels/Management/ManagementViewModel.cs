using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Application.Management.Currencies;
using SpareParts.Desktop.Abstractions.Parts;
using SpareParts.Desktop.Abstractions.UsedCars;
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
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

namespace SpareParts.Desktop.Wpf
{
    public class ManagementViewModel : INotifyPropertyChanged
    {
        private readonly ManagementCoordinator _coordinator;
        private readonly ManagementStatusCenter _statusCenter = new();
        private string _baseCurrencyCode = "USD";
        private string _counterCurrencyCode = "USD";
        private string _displayCurrencyCode = "USD";
        private decimal _defaultCounterRate = 1m;

        public CustomerManagementViewModel CustomersFeature { get; }
        public SupplierManagementViewModel SuppliersFeature { get; }
        public BrandManagementViewModel BrandsFeature { get; }
        public PartManagementViewModel PartsFeature { get; }
        public PartRequestsManagementViewModel PartRequestsFeature { get; }
        public CarModelManagementViewModel CarModelsFeature { get; }
        public LocationManagementViewModel LocationsFeature { get; }
        public WarehouseManagementViewModel WarehousesFeature { get; }
        public CurrencyRatesManagementViewModel CurrencyRatesFeature { get; }
        public UsedCarsManagementViewModel UsedCarsFeature { get; }
        public TransactionTypeManagementViewModel TransactionTypesFeature { get; }
        public AccountingViewModel AccountingVm { get; }
        public ExcelManagerViewModel ExcelManagerFeature { get; }

        public UsersViewModel UsersVm { get; }
        public RolesViewModel RolesVm { get; }
        public ObservableCollection<CategoryDto> Categories { get; } = new();
        public ObservableCollection<AppConstantDto> AppConstants { get; } = new();

        public ObservableCollection<CustomerDto> Customers => CustomersFeature.Customers;
        public ObservableCollection<SupplierDto> Suppliers => SuppliersFeature.Suppliers;
        public ObservableCollection<BrandDto> Brands => BrandsFeature.Brands;
        public ObservableCollection<PartDto> Parts => PartsFeature.Parts;
        public ObservableCollection<PartRequestDto> PartRequests => PartRequestsFeature.Requests;
        public ObservableCollection<CarModelDto> CarModels => CarModelsFeature.CarModels;
        public ObservableCollection<CarBrandDto> CarBrands => CarModelsFeature.CarBrands;
        public ObservableCollection<LocationDto> Locations => LocationsFeature.Locations;
        public ObservableCollection<WarehouseDto> Warehouses => WarehousesFeature.Warehouses;
        public ObservableCollection<CurrencyRateDto> CurrencyRates => CurrencyRatesFeature.CurrencyRates;
        public ObservableCollection<CurrencyRateDisplayRow> CurrencyRateRows => CurrencyRatesFeature.CurrencyRateRows;
        public ObservableCollection<UsedCarModelOption> UsedCarModelOptions => UsedCarsFeature.UsedCarModelOptions;
        public ObservableCollection<string> UsedCarCurrencyCodes => UsedCarsFeature.CurrencyCodes;
        public ObservableCollection<string> LocationCurrencyCodes => CurrencyRatesFeature.CurrencyCodes;
        public ObservableCollection<TransactionTypeDto> TransactionTypes => TransactionTypesFeature.TransactionTypes;
        public ObservableCollection<UsedCarEntry> UsedCars => UsedCarsFeature.UsedCars;
        public ObservableCollection<AccountingAccountRow> AccountingAccounts { get; } = new();
        public ObservableCollection<AccountingAccountRow> ConfiguredAccountingAccounts { get; } = new();
        public ObservableCollection<AccountingPostingRuleRow> AccountingPostingRules { get; } = new();


        public bool CanViewSupplierTab => SuppliersFeature.CanViewSupplierTab;
        public bool CanViewCurrencyTab => CurrencyRatesFeature.CanViewCurrencyTab;
        public bool CanViewTransactionTypesTab => TransactionTypesFeature.CanViewTransactionTypesTab;
        public bool CanEditSupplier => SuppliersFeature.CanEditSupplier;
        public bool CanModifySupplier => SuppliersFeature.CanModifySupplier;
        public bool CanDeleteSupplier => SuppliersFeature.CanDeleteSupplier;
        public bool CanSaveSupplier => SuppliersFeature.CanSaveSupplier;
        public string BaseCurrencyCode => _baseCurrencyCode;
        public string CounterCurrencyCode => _counterCurrencyCode;
        public string DisplayCurrencyCode => _displayCurrencyCode;
        public string CurrencyRatesSummary => $"Base {BaseCurrencyCode} | Counter {CounterCurrencyCode} | Display {DisplayCurrencyCode}";
        public int AccountingAccountCount => AccountingAccounts.Count;
        public int ActiveTransactionTypeCount => TransactionTypes.Count(item => item.IsActive);
        public decimal CustomerOpeningBalanceTotal => decimal.Round(Customers.Sum(customer => customer.OpeningBalance), 2, MidpointRounding.AwayFromZero);
        public decimal SupplierOpeningBalanceTotal => decimal.Round(Suppliers.Sum(supplier => supplier.OpeningBalance), 2, MidpointRounding.AwayFromZero);
        public string AccountingChartSummary => $"{AccountingAccountCount} seeded account(s) are available for the chart of accounts.";
        public string AccountingPostingSummary => $"{AccountingPostingRules.Count} posting flow(s) are currently wired in backend services.";
        public string AccountingOperationsSummary => $"{ActiveTransactionTypeCount} active transaction type(s) and {CurrencyRates.Count} currency rate snapshot(s) support the accounting workflow.";

        public CustomerDto? SelectedCustomer { get => CustomersFeature.SelectedCustomer; set => CustomersFeature.SelectedCustomer = value; }
        public SupplierDto? SelectedSupplier
        {
            get => SuppliersFeature.SelectedSupplier;
            set => SuppliersFeature.SelectedSupplier = value;
        }
        public BrandDto? SelectedBrand { get => BrandsFeature.SelectedBrand; set => BrandsFeature.SelectedBrand = value; }
        public PartDto? SelectedPart { get => PartsFeature.SelectedPart; set => PartsFeature.SelectedPart = value; }
        public PartRequestDto? SelectedPartRequest { get => PartRequestsFeature.SelectedRequest; set => PartRequestsFeature.SelectedRequest = value; }
        public CarBrandDto? SelectedCarBrand
        {
            get => CarModelsFeature.SelectedCarBrand;
            set => CarModelsFeature.SelectedCarBrand = value;
        }
        public CarModelDto? SelectedCarModel { get => CarModelsFeature.SelectedCarModel; set => CarModelsFeature.SelectedCarModel = value; }
        public LocationDto? SelectedLocation
        {
            get => LocationsFeature.SelectedLocation;
            set => LocationsFeature.SelectedLocation = value;
        }
        public WarehouseDto? SelectedWarehouse { get => WarehousesFeature.SelectedWarehouse; set => WarehousesFeature.SelectedWarehouse = value; }
        public TransactionTypeDto? SelectedTransactionType
        {
            get => TransactionTypesFeature.SelectedTransactionType;
            set => TransactionTypesFeature.SelectedTransactionType = value;
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
        public int? NewRequestPartId { get => PartRequestsFeature.NewRequestPartId; set { PartRequestsFeature.NewRequestPartId = value; OnPropertyChanged(nameof(NewRequestPartId)); } }
        public int? NewRequestCustomerId { get => PartRequestsFeature.NewRequestCustomerId; set { PartRequestsFeature.NewRequestCustomerId = value; OnPropertyChanged(nameof(NewRequestCustomerId)); } }
        public string NewRequestCustomerName { get => PartRequestsFeature.NewRequestCustomerName; set { PartRequestsFeature.NewRequestCustomerName = value; OnPropertyChanged(nameof(NewRequestCustomerName)); } }
        public string NewRequestCustomerPhone { get => PartRequestsFeature.NewRequestCustomerPhone; set { PartRequestsFeature.NewRequestCustomerPhone = value; OnPropertyChanged(nameof(NewRequestCustomerPhone)); } }
        public string NewRequestPartName { get => PartRequestsFeature.NewRequestPartName; set { PartRequestsFeature.NewRequestPartName = value; OnPropertyChanged(nameof(NewRequestPartName)); } }
        public string NewRequestOem { get => PartRequestsFeature.NewRequestOem; set { PartRequestsFeature.NewRequestOem = value; OnPropertyChanged(nameof(NewRequestOem)); } }
        public string NewRequestVehicleDetails { get => PartRequestsFeature.NewRequestVehicleDetails; set { PartRequestsFeature.NewRequestVehicleDetails = value; OnPropertyChanged(nameof(NewRequestVehicleDetails)); } }
        public int NewRequestQuantity { get => PartRequestsFeature.NewRequestQuantity; set { PartRequestsFeature.NewRequestQuantity = value; OnPropertyChanged(nameof(NewRequestQuantity)); } }
        public string NewRequestNotes { get => PartRequestsFeature.NewRequestNotes; set { PartRequestsFeature.NewRequestNotes = value; OnPropertyChanged(nameof(NewRequestNotes)); } }
        public int ReadyPartRequestCount => PartRequestsFeature.ReadyRequestCount;
        public int OpenPartRequestCount => PartRequestsFeature.OpenRequestCount;
        public string PartRequestsBoardSummary => PartRequestsFeature.BoardSummary;

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
        public string NewTransactionSerialNumberFormat { get => TransactionTypesFeature.NewTransactionSerialNumberFormat; set { TransactionTypesFeature.NewTransactionSerialNumberFormat = value; OnPropertyChanged(nameof(NewTransactionSerialNumberFormat)); } }
        public long NewTransactionStartNumber { get => TransactionTypesFeature.NewTransactionStartNumber; set { TransactionTypesFeature.NewTransactionStartNumber = value; OnPropertyChanged(nameof(NewTransactionStartNumber)); } }
        public long NewTransactionCurrentNumber { get => TransactionTypesFeature.NewTransactionCurrentNumber; set { TransactionTypesFeature.NewTransactionCurrentNumber = value; OnPropertyChanged(nameof(NewTransactionCurrentNumber)); } }
        public bool NewTransactionIsActive { get => TransactionTypesFeature.NewTransactionIsActive; set { TransactionTypesFeature.NewTransactionIsActive = value; OnPropertyChanged(nameof(NewTransactionIsActive)); } }

        public string Status => _statusCenter.Status;
        public ObservableCollection<StatusMessage> StatusMessages => _statusCenter.StatusMessages;
        public Brush StatusBrush => _statusCenter.StatusBrush;
        private bool _isLoading;
        private AccountingAccountRow? _selectedAccountingAccount;
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

        public bool IsGeneratingPartNotes => PartsFeature.IsGeneratingPartNotes;
        public string PartAiButtonText => PartsFeature.PartAiButtonText;
        public bool IsImportingParts => PartsFeature.IsImportingParts;
        public string PartImportButtonText => PartsFeature.PartImportButtonText;

        public ICommand LoadAllCommand { get; }
        public ICommand SaveCustomerCommand => CustomersFeature.SaveCommand;
        public ICommand DeleteCustomerCommand => CustomersFeature.DeleteCommand;
        public ICommand SaveSupplierCommand => SuppliersFeature.SaveCommand;
        public ICommand DeleteSupplierCommand => SuppliersFeature.DeleteCommand;
        public ICommand SaveBrandCommand => BrandsFeature.SaveCommand;
        public ICommand DeleteBrandCommand => BrandsFeature.DeleteCommand;
        public ICommand SavePartCommand => PartsFeature.SaveCommand;
        public ICommand DeletePartCommand => PartsFeature.DeleteCommand;
        public ICommand SavePartRequestCommand => PartRequestsFeature.SaveCommand;
        public ICommand DeletePartRequestCommand => PartRequestsFeature.DeleteCommand;
        public ICommand MarkPartRequestContactedCommand => PartRequestsFeature.MarkContactedCommand;
        public ICommand MarkPartRequestFulfilledCommand => PartRequestsFeature.MarkFulfilledCommand;
        public ICommand CancelPartRequestCommand => PartRequestsFeature.CancelCommand;
        public ICommand ReopenPartRequestCommand => PartRequestsFeature.ReopenCommand;
        public ICommand ImportPartsFromExcelCommand => PartsFeature.ImportFromExcelCommand;
        public ICommand SaveCarBrandCommand => CarModelsFeature.SaveCarBrandCommand;
        public ICommand DeleteCarBrandCommand => CarModelsFeature.DeleteCarBrandCommand;
        public ICommand SaveCarModelCommand => CarModelsFeature.SaveCarModelCommand;
        public ICommand DeleteCarModelCommand => CarModelsFeature.DeleteCarModelCommand;
        public ICommand SaveLocationCommand => LocationsFeature.SaveCommand;
        public ICommand DeleteLocationCommand => LocationsFeature.DeleteCommand;
        public ICommand SaveWarehouseCommand => WarehousesFeature.SaveCommand;
        public ICommand DeleteWarehouseCommand => WarehousesFeature.DeleteCommand;
        public ICommand SaveTransactionTypeCommand => TransactionTypesFeature.SaveCommand;
        public ICommand DeleteTransactionTypeCommand => TransactionTypesFeature.DeleteCommand;
        public ICommand GeneratePartNotesCommand => PartsFeature.GeneratePartNotesCommand;
        public ICommand StartNewManagementItemCommand { get; }

        public ManagementViewModel(
            ICrudApiClient crudApi,
            IAccountingApiClient accountingApi,
            ICarCatalogApiClient carCatalogApi,
            IPartsApiClient partsApi,
            UsersViewModel usersVm,
            RolesViewModel rolesVm,
            IFilePickerService filePickerService,
            IUserNotificationService notificationService,
            IUsedCarWorkspaceService usedCarWorkspaceService,
            IPartWorkspaceService partWorkspaceService,
            bool canViewSupplierTab = false,
            bool canEditSupplier = false,
            bool canModifySupplier = false,
            bool canDeleteSupplier = false)
        {
            UsersVm = usersVm;
            RolesVm = rolesVm;
            AccountingVm = new AccountingViewModel(accountingApi);

            _coordinator = new ManagementCoordinator(
                crudApi,
                carCatalogApi,
                partsApi);
            ExcelManagerFeature = new ExcelManagerViewModel(
                _coordinator,
                BuildExcelImportExecutionContext,
                LoadAllAsync,
                SetStatus);
            CurrencyRatesFeature = new CurrencyRatesManagementViewModel(
                new CurrencyRateProjectionService(),
                LoadAllAsync,
                ExcelManagerFeature.ImportTableCommand);

            var stdCtx = new ManagementFeatureContext(_coordinator, LoadAllAsync, SetStatus, ExcelManagerFeature.ImportTableCommand);
            var counterCtx = new ManagementFeatureContext(_coordinator, LoadAllAsync, SetStatus, ExcelManagerFeature.ImportTableCommand, () => _counterCurrencyCode);
            var baseCurrencyCtx = new ManagementFeatureContext(_coordinator, LoadAllAsync, SetStatus, ExcelManagerFeature.ImportTableCommand, () => _baseCurrencyCode);
            var noImportCtx = new ManagementFeatureContext(_coordinator, LoadAllAsync, SetStatus);

            CustomersFeature = new CustomerManagementViewModel(stdCtx);
            SuppliersFeature = new SupplierManagementViewModel(stdCtx);
            BrandsFeature = new BrandManagementViewModel(stdCtx);
            PartsFeature = new PartManagementViewModel(baseCurrencyCtx, filePickerService, notificationService, partWorkspaceService);
            PartRequestsFeature = new PartRequestsManagementViewModel(noImportCtx);
            CarModelsFeature = new CarModelManagementViewModel(stdCtx);
            LocationsFeature = new LocationManagementViewModel(counterCtx);
            WarehousesFeature = new WarehouseManagementViewModel(stdCtx);
            TransactionTypesFeature = new TransactionTypeManagementViewModel(counterCtx);

            UsedCarsFeature = new UsedCarsManagementViewModel(
                _coordinator,
                CurrencyRatesFeature,
                usedCarWorkspaceService,
                notificationService,
                LoadAllAsync,
                SetStatus,
                ExcelManagerFeature.ImportTableCommand);
            SetSupplierPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);
            CustomersFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            SuppliersFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            BrandsFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            PartsFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            PartRequestsFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            CarModelsFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            LocationsFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            WarehousesFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            TransactionTypesFeature.PropertyChanged += ForwardFeaturePropertyChanged;
            CurrencyRatesFeature.PropertyChanged += ForwardFeaturePropertyChanged;

            LoadAllCommand = new RelayCommand(_ => _ = LoadAllAsync());
            StartNewManagementItemCommand = new RelayCommand(StartNewManagementItem);
        }

        public void SetTabPermissions(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier, bool canViewCurrencyTab, bool canViewTransactionTypesTab)
        {
            SuppliersFeature.SetPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);
            CurrencyRatesFeature.CanViewCurrencyTab = canViewCurrencyTab;
            TransactionTypesFeature.CanViewTransactionTypesTab = canViewTransactionTypesTab;
        }

        public void SetSupplierPermissions(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier)
        {
            SuppliersFeature.SetPermissions(canViewSupplierTab, canEditSupplier, canModifySupplier, canDeleteSupplier);
        }


        public async Task LoadAllAsync()
        {
            if (IsLoading)
            {
                SetStatus("Loading is already in progress…", true);
                return;
            }

            IsLoading = true;
            SetStatus("Loading…", true);
            try
            {
                var loadResult = await _coordinator.LoadAllAsync(RolesVm);
                await AccountingVm.LoadSetupAsync();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ApplyCurrencyDefaults(loadResult.AppConstants);
                    Replace(AppConstants, loadResult.AppConstants);
                    Replace(Customers, loadResult.Customers);
                    Replace(Suppliers, loadResult.Suppliers);
                    Replace(Brands, loadResult.Brands);
                    Replace(CarBrands, loadResult.CarBrands);
                    Replace(Categories, loadResult.Categories);
            PartsFeature.LoadParts(loadResult.Parts, loadResult.PartRequests);
                    PartRequestsFeature.LoadReferenceData(loadResult.Customers, loadResult.Parts);
                    PartRequestsFeature.Load(loadResult.PartRequests);
                    Replace(CarModels, loadResult.CarModels);
                    Replace(Locations, loadResult.Locations);
                    Replace(Warehouses, loadResult.Warehouses);
                    CurrencyRatesFeature.Load(loadResult.CurrencyRates, _baseCurrencyCode, _counterCurrencyCode, _displayCurrencyCode, _defaultCounterRate);
                    PartsFeature.LoadReferenceData(loadResult.Categories, loadResult.Brands, loadResult.CurrencyRates);
                    LocationsFeature.LoadCurrencyCodes(CurrencyRatesFeature.CurrencyCodes);
                    EnsureLocationFormCurrencySelection();
                    UsedCarsFeature.LoadReferenceData(loadResult.CarModels, loadResult.Suppliers, loadResult.Locations);
                    UsedCarsFeature.LoadUsedCars(loadResult.UsedCars);
                    Replace(TransactionTypes, loadResult.TransactionTypes);
                    ExcelManagerFeature.UpdateRuntimeData(loadResult.AppConstants.ToList());
                    RaiseAccountingDashboardProps();
                }, DispatcherPriority.Background);

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
            => target.ReplaceWith(source);

        private void ApplyCurrencyDefaults(IEnumerable<AppConstantDto> constants)
        {
            var byKey = constants.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            _baseCurrencyCode = ResolveCurrencyCode(byKey, "BaseCurrencyCode")
                ?? ResolveCurrencyCode(byKey, "DefaultCurrencyCode")
                ?? "USD";
            _counterCurrencyCode = ResolveCurrencyCode(byKey, "CounterCurrencyCode")
                ?? _baseCurrencyCode;
            _displayCurrencyCode = ResolveCurrencyCode(byKey, "DisplayCurrencyCode")
                ?? _counterCurrencyCode;
            if (byKey.TryGetValue("DefaultCounterRate", out var defaultCounterRate)
                && decimal.TryParse(defaultCounterRate, out var parsedRate)
                && parsedRate > 0)
            {
                _defaultCounterRate = parsedRate;
            }

            OnPropertyChanged(nameof(BaseCurrencyCode));
            OnPropertyChanged(nameof(CounterCurrencyCode));
            OnPropertyChanged(nameof(DisplayCurrencyCode));
            OnPropertyChanged(nameof(CurrencyRatesSummary));
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

        private void EnsureLocationFormCurrencySelection()
        {
            if (NormalizeCurrencyCode(LocationsFeature.NewLocationShippingFeesCurrencyCode) != null)
            {
                return;
            }

            LocationsFeature.NewLocationShippingFeesCurrencyCode = _counterCurrencyCode;
            OnPropertyChanged(nameof(NewLocationShippingFeesCurrencyCode));
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

        private ExcelImportExecutionContext BuildExcelImportExecutionContext()
        {
            return new ExcelImportExecutionContext
            {
                Customers = Customers.ToList(),
                Suppliers = Suppliers.ToList(),
                Brands = Brands.ToList(),
                Categories = Categories.ToList(),
                CarBrands = CarBrands.ToList(),
                CarModels = CarModels.ToList(),
                Locations = Locations.ToList(),
                Warehouses = Warehouses.ToList(),
                TransactionTypes = TransactionTypes.ToList(),
                UsedCars = UsedCars.ToList(),
                Accounts = AccountingVm.Accounts.ToList(),
                AccountTypes = AccountingVm.AccountTypes.ToList(),
                BaseCurrencyCode = _baseCurrencyCode,
                CounterCurrencyCode = _counterCurrencyCode,
                DefaultCounterRate = _defaultCounterRate
            };
        }

        private void StartNewManagementItem(object? parameter)
        {
            switch (parameter as string)
            {
                case "Customer":
                    CustomersFeature.StartNew();
                    break;
                case "Supplier":
                    SuppliersFeature.StartNew();
                    break;
                case "Brand":
                    BrandsFeature.StartNew();
                    break;
                case "Part":
                    PartsFeature.StartNew();
                    break;
                case "PartRequest":
                    PartRequestsFeature.StartNew();
                    break;
                case "CarBrand":
                    CarModelsFeature.StartNewCarBrand();
                    break;
                case "CarModel":
                    CarModelsFeature.StartNewCarModel();
                    break;
                case "Location":
                    LocationsFeature.StartNew();
                    break;
                case "Warehouse":
                    WarehousesFeature.StartNew();
                    break;
                case "TransactionType":
                    TransactionTypesFeature.StartNew();
                    break;
                case "UsedCar":
                    UsedCarsFeature.StartNew();
                    break;
            }
        }

        private void ForwardFeaturePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                OnPropertyChanged(e.PropertyName!);
            }

            if (sender == PartRequestsFeature
                && (e.PropertyName == nameof(PartRequestsManagementViewModel.ReadyRequestCount)
                    || e.PropertyName == nameof(PartRequestsManagementViewModel.OpenRequestCount)
                    || e.PropertyName == nameof(PartRequestsManagementViewModel.BoardSummary)))
            {
                OnPropertyChanged(nameof(ReadyPartRequestCount));
                OnPropertyChanged(nameof(OpenPartRequestCount));
                OnPropertyChanged(nameof(PartRequestsBoardSummary));
            }
        }

        private void SetStatus(string message, bool isSuccess) => _statusCenter.SetStatus(message, isSuccess);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
