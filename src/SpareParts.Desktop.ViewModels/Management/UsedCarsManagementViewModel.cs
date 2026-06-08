using SpareParts.Application.Management.UsedCars;
using SpareParts.Application.Management.Currencies;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.UsedCars;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

public sealed class UsedCarsManagementViewModel : INotifyPropertyChanged
{
    private readonly ManagementCoordinator _coordinator;
    private readonly CurrencyRatesManagementViewModel _currencyRatesFeature;
    private readonly IUsedCarWorkspaceService _usedCarWorkspaceService;
    private readonly IUserNotificationService _notificationService;
    private readonly Func<Task> _refreshAsync;
    private readonly Action<string, bool> _setStatus;
    private readonly UsedCarValuationService _valuationService;
    private UsedCarEntry? _selectedUsedCar;
    private string _usedCarFilterText = string.Empty;
    private string _usedCarStatusFilter = "All donors";
    private string _newUsedCarBarcode = string.Empty;
    private string _newUsedCarName = string.Empty;
    private int _newUsedCarModelYear;
    private int? _newUsedCarCarModelId;
    private int? _newUsedCarSupplierId;
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
    private decimal _newUsedCarRepairs;
    private decimal _newUsedCarTotalBeforeShipping;
    private decimal _newUsedCarGrandTotalBase;
    private decimal _newUsedCarGrandTotalCounter;
    private decimal _newUsedCarExpectedSellThroughRate = 0.80m;

    public UsedCarsManagementViewModel(
        ManagementCoordinator coordinator,
        CurrencyRatesManagementViewModel currencyRatesFeature,
        IUsedCarWorkspaceService usedCarWorkspaceService,
        IUserNotificationService notificationService,
        Func<Task> refreshAsync,
        Action<string, bool> setStatus,
        ICommand? importTableCommand = null)
    {
        _coordinator = coordinator;
        _currencyRatesFeature = currencyRatesFeature;
        _usedCarWorkspaceService = usedCarWorkspaceService;
        _notificationService = notificationService;
        _refreshAsync = refreshAsync;
        _setStatus = setStatus;
        _valuationService = new UsedCarValuationService(new CurrencyRateProjectionService());

        _currencyRatesFeature.PropertyChanged += CurrencyRatesFeatureOnPropertyChanged;
        UsedCars.CollectionChanged += UsedCars_CollectionChanged;

        StartNewCommand = new RelayCommand(_ => StartNew());
        OpenGalleryCommand = new RelayCommand(_ => OpenUsedCarGallery());
        OpenPartsCommand = new RelayCommand(_ => OpenUsedCarParts());
        GenerateListingCommand = new RelayCommand(_ => OpenUsedCarListingPackage());
        SaveCommand = new RelayCommand(_ => _ = SaveUsedCarAsync());
        DeleteCommand = new RelayCommand(_ => _ = RemoveSelectedUsedCarAsync());
        RefreshCommand = new RelayCommand(_ => _ = _refreshAsync());
        ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.UsedCars"));
    }

    public ObservableCollection<UsedCarEntry> UsedCars { get; } = new BulkObservableCollection<UsedCarEntry>();
    public ObservableCollection<UsedCarEntry> FilteredUsedCars { get; } = new BulkObservableCollection<UsedCarEntry>();
    public ObservableCollection<string> UsedCarStatusFilters { get; } = new()
    {
        "All donors",
        "Action needed",
        "Gap open",
        "Needs part links",
        "Profit unlocked",
        "In transit",
        "Wholesale sold"
    };
    public ObservableCollection<UsedCarModelOption> UsedCarModelOptions { get; } = new BulkObservableCollection<UsedCarModelOption>();
    public ObservableCollection<string> CurrencyCodes { get; } = new BulkObservableCollection<string>();
    public ObservableCollection<SupplierDto> Suppliers { get; } = new BulkObservableCollection<SupplierDto>();
    public ObservableCollection<LocationDto> Locations { get; } = new BulkObservableCollection<LocationDto>();
    public ObservableCollection<CarModelDto> CarModels { get; } = new BulkObservableCollection<CarModelDto>();

    public ICommand StartNewCommand { get; }
    public ICommand OpenGalleryCommand { get; }
    public ICommand OpenPartsCommand { get; }
    public ICommand GenerateListingCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ImportFromExcelCommand { get; }

    public string UsedCarPriceLabel => $"Price ({NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _currencyRatesFeature.BaseCurrencyCode})";
    public string UsedCarBasePriceLabel => $"Price Base ({_currencyRatesFeature.BaseCurrencyCode})";
    public string UsedCarCounterPriceLabel => $"Price Counter ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarTransportationLabel => $"Transportation ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarPartOutLabel => $"Part-Out ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarShippingLabel => $"Shipping ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarCustomsLabel => $"Customs ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarRepairsLabel => $"Repairs ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarTotalBeforeShippingLabel => $"Total Before Shipping ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarGrandTotalBaseLabel => $"Grand Total Base ({_currencyRatesFeature.BaseCurrencyCode})";
    public string UsedCarGrandTotalCounterLabel => $"Grand Total Counter ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarsTotalBaseLabel => $"Total Base Amount ({_currencyRatesFeature.BaseCurrencyCode})";
    public string UsedCarsTotalCounterLabel => $"Total Counter Amount ({_currencyRatesFeature.CounterCurrencyCode})";
    public string UsedCarsTotalDisplayLabel => $"Total Amount ({_currencyRatesFeature.DisplayCurrencyCode})";
    public string UsedCarsNetProfitLossDisplayLabel => $"Net P/L ({_currencyRatesFeature.DisplayCurrencyCode})";
    public decimal UsedCarsTotalBaseAmount => decimal.Round(UsedCars.Sum(entry => entry.GrandTotalBase), 2, MidpointRounding.AwayFromZero);
    public decimal UsedCarsTotalCounterAmount => decimal.Round(UsedCars.Sum(entry => entry.GrandTotalCounter), 2, MidpointRounding.AwayFromZero);
    public decimal UsedCarsNetProfitLossBaseAmount => decimal.Round(UsedCars.Sum(entry => entry.NetProfitLossBase), 2, MidpointRounding.AwayFromZero);
    public decimal UsedCarsTotalDisplayAmount => decimal.Round(UsedCars.Sum(entry => entry.GrandTotalDisplay), 2, MidpointRounding.AwayFromZero);
    public decimal UsedCarsNetProfitLossDisplayAmount => decimal.Round(UsedCars.Sum(entry => entry.NetProfitLossDisplay), 2, MidpointRounding.AwayFromZero);
    public int FilteredUsedCarsCount => FilteredUsedCars.Count;
    public int UsedCarsReadyForTeardownCount => UsedCars.Count(entry => entry.IsReceived && entry.IsShipped && !entry.IsWholesaleSold);
    public int UsedCarsProfitUnlockedCount => UsedCars.Count(entry => entry.FullCostBase > 0m && entry.ProfitMapBreakEvenGapBase <= 0m);
    public int UsedCarsBreakEvenRiskCount => UsedCars.Count(entry => entry.ProfitMapBreakEvenGapBase > 0m);
    public int UsedCarsNeedsPartLinksCount => UsedCars.Count(entry => entry.PartsRemovedCount <= 0 && !entry.IsWholesaleSold);
    public decimal UsedCarsBreakEvenGapDisplayAmount => decimal.Round(UsedCars.Sum(entry => entry.ProfitMapBreakEvenGapDisplay), 2, MidpointRounding.AwayFromZero);
    public string UsedCarsBreakEvenGapDisplayLabel => $"Break-Even Gap ({_currencyRatesFeature.DisplayCurrencyCode})";
    public string SelectedUsedCarActionSummary => SelectedUsedCar is null
        ? "Select a donor car to see the next action."
        : BuildUsedCarActionSummary(SelectedUsedCar);
    public string SelectedUsedCarOperationalSummary => SelectedUsedCar is null
        ? "No donor selected."
        : BuildUsedCarOperationalSummary(SelectedUsedCar);

    public string UsedCarFilterText
    {
        get => _usedCarFilterText;
        set
        {
            if (_usedCarFilterText == value)
            {
                return;
            }

            _usedCarFilterText = value ?? string.Empty;
            OnPropertyChanged(nameof(UsedCarFilterText));
            RefreshUsedCarFilters();
        }
    }

    public string UsedCarStatusFilter
    {
        get => _usedCarStatusFilter;
        set
        {
            var nextValue = string.IsNullOrWhiteSpace(value) ? "All donors" : value;
            if (string.Equals(_usedCarStatusFilter, nextValue, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _usedCarStatusFilter = nextValue;
            OnPropertyChanged(nameof(UsedCarStatusFilter));
            RefreshUsedCarFilters();
        }
    }

    public UsedCarEntry? SelectedUsedCar
    {
        get => _selectedUsedCar;
        set
        {
            if (_selectedUsedCar == value)
            {
                return;
            }

            _selectedUsedCar = value;
            OnPropertyChanged(nameof(SelectedUsedCar));
            RaiseDonorCommandProps();

            if (value == null)
            {
                return;
            }

            _newUsedCarCarModelId = value.CarModelId
                ?? CarModels.FirstOrDefault(model =>
                    string.Equals(model.Name, value.Car, StringComparison.OrdinalIgnoreCase))?.Id;
            _newUsedCarBarcode = value.Barcode ?? string.Empty;
            _newUsedCarSupplierId = value.SupplierId;
            _newUsedCarName = value.Car;
            _newUsedCarModelYear = value.ModelYear;
            _newUsedCarPriceCurrency = NormalizeCurrencyCode(value.PriceCurrency) ?? _currencyRatesFeature.BaseCurrencyCode;
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
            _newUsedCarRepairs = value.Repairs;
            _newUsedCarTotalBeforeShipping = value.TotalBeforeShipping;
            _newUsedCarGrandTotalBase = value.GrandTotalBase;
            _newUsedCarGrandTotalCounter = value.GrandTotalCounter;
            _newUsedCarExpectedSellThroughRate = value.ExpectedSellThroughRate > 0m ? value.ExpectedSellThroughRate : 0.80m;

            RaiseEditorProps();
        }
    }

    public string NewUsedCarBarcode
    {
        get => _newUsedCarBarcode;
        set
        {
            if (_newUsedCarBarcode == value)
            {
                return;
            }

            _newUsedCarBarcode = value;
            OnPropertyChanged(nameof(NewUsedCarBarcode));
        }
    }

    public string NewUsedCarName
    {
        get => _newUsedCarName;
        set
        {
            if (_newUsedCarName == value)
            {
                return;
            }

            _newUsedCarName = value;
            OnPropertyChanged(nameof(NewUsedCarName));
        }
    }

    public int NewUsedCarModelYear
    {
        get => _newUsedCarModelYear;
        set
        {
            if (_newUsedCarModelYear == value)
            {
                return;
            }

            _newUsedCarModelYear = value;
            OnPropertyChanged(nameof(NewUsedCarModelYear));
        }
    }

    public int? NewUsedCarCarModelId
    {
        get => _newUsedCarCarModelId;
        set
        {
            if (_newUsedCarCarModelId == value)
            {
                return;
            }

            _newUsedCarCarModelId = value;
            OnPropertyChanged(nameof(NewUsedCarCarModelId));
            SyncUsedCarFromSelectedModel();
        }
    }

    public int? NewUsedCarSupplierId
    {
        get => _newUsedCarSupplierId;
        set
        {
            if (_newUsedCarSupplierId == value)
            {
                return;
            }

            _newUsedCarSupplierId = value;
            OnPropertyChanged(nameof(NewUsedCarSupplierId));
        }
    }

    public decimal NewUsedCarPrice
    {
        get => _newUsedCarPrice;
        set
        {
            if (_newUsedCarPrice == value)
            {
                return;
            }

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
            var normalized = NormalizeCurrencyCode(value) ?? _currencyRatesFeature.BaseCurrencyCode;
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
        private set
        {
            if (_newUsedCarPriceBase == value)
            {
                return;
            }

            _newUsedCarPriceBase = value;
            OnPropertyChanged(nameof(NewUsedCarPriceBase));
        }
    }

    public decimal NewUsedCarPriceCounter
    {
        get => _newUsedCarPriceCounter;
        private set
        {
            if (_newUsedCarPriceCounter == value)
            {
                return;
            }

            _newUsedCarPriceCounter = value;
            OnPropertyChanged(nameof(NewUsedCarPriceCounter));
        }
    }

    public int? NewUsedCarLocationId
    {
        get => _newUsedCarLocationId;
        set
        {
            if (_newUsedCarLocationId == value)
            {
                return;
            }

            _newUsedCarLocationId = value;
            OnPropertyChanged(nameof(NewUsedCarLocationId));
            SyncUsedCarTransportationFromSelectedLocation();
        }
    }

    public decimal NewUsedCarTransportation
    {
        get => _newUsedCarTransportation;
        private set
        {
            if (_newUsedCarTransportation == value)
            {
                return;
            }

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
            if (_newUsedCarIsReceived == value)
            {
                return;
            }

            _newUsedCarIsReceived = value;
            OnPropertyChanged(nameof(NewUsedCarIsReceived));
        }
    }

    public bool NewUsedCarIsShipped
    {
        get => _newUsedCarIsShipped;
        set
        {
            if (_newUsedCarIsShipped == value)
            {
                return;
            }

            _newUsedCarIsShipped = value;
            OnPropertyChanged(nameof(NewUsedCarIsShipped));
        }
    }

    public decimal NewUsedCarPartOut
    {
        get => _newUsedCarPartOut;
        set
        {
            if (_newUsedCarPartOut == value)
            {
                return;
            }

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
            if (_newUsedCarShipping == value)
            {
                return;
            }

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
            if (_newUsedCarCustoms == value)
            {
                return;
            }

            _newUsedCarCustoms = value;
            OnPropertyChanged(nameof(NewUsedCarCustoms));
            RecalculateUsedCarTotals();
        }
    }

    public decimal NewUsedCarRepairs
    {
        get => _newUsedCarRepairs;
        set
        {
            if (_newUsedCarRepairs == value)
            {
                return;
            }

            _newUsedCarRepairs = value;
            OnPropertyChanged(nameof(NewUsedCarRepairs));
            RecalculateUsedCarTotals();
        }
    }

    public decimal NewUsedCarTotalBeforeShipping
    {
        get => _newUsedCarTotalBeforeShipping;
        private set
        {
            if (_newUsedCarTotalBeforeShipping == value)
            {
                return;
            }

            _newUsedCarTotalBeforeShipping = value;
            OnPropertyChanged(nameof(NewUsedCarTotalBeforeShipping));
        }
    }

    public decimal NewUsedCarGrandTotalBase
    {
        get => _newUsedCarGrandTotalBase;
        private set
        {
            if (_newUsedCarGrandTotalBase == value)
            {
                return;
            }

            _newUsedCarGrandTotalBase = value;
            OnPropertyChanged(nameof(NewUsedCarGrandTotalBase));
        }
    }

    public decimal NewUsedCarGrandTotalCounter
    {
        get => _newUsedCarGrandTotalCounter;
        private set
        {
            if (_newUsedCarGrandTotalCounter == value)
            {
                return;
            }

            _newUsedCarGrandTotalCounter = value;
            OnPropertyChanged(nameof(NewUsedCarGrandTotalCounter));
        }
    }

    public decimal NewUsedCarExpectedSellThroughRate
    {
        get => _newUsedCarExpectedSellThroughRate;
        set
        {
            if (_newUsedCarExpectedSellThroughRate == value)
            {
                return;
            }

            _newUsedCarExpectedSellThroughRate = value;
            OnPropertyChanged(nameof(NewUsedCarExpectedSellThroughRate));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void LoadReferenceData(
        IEnumerable<CarModelDto> carModels,
        IEnumerable<SupplierDto> suppliers,
        IEnumerable<LocationDto> locations)
    {
        Replace(CarModels, carModels);
        Replace(Suppliers, suppliers);
        Replace(Locations, locations);
        Replace(
            UsedCarModelOptions,
            CarModels
                .OrderBy(model => model.CarBrandName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
                .Select(model => new UsedCarModelOption
                {
                    Id = model.Id,
                    CarBrandName = model.CarBrandName,
                    Name = model.Name,
                    BodyType = model.BodyType
                }));

        RefreshCurrencyCodes();
    }

    public void LoadUsedCars(IEnumerable<UsedCarDto> usedCars)
    {
        var selectedUsedCarId = SelectedUsedCar?.Id;
        Replace(UsedCars, usedCars.Select(MapUsedCar));
        ApplyDisplayCurrencyToUsedCars();

        if (selectedUsedCarId.HasValue)
        {
            SelectedUsedCar = UsedCars.FirstOrDefault(entry => entry.Id == selectedUsedCarId.Value);
        }

        RefreshUsedCarFilters();
        RaiseSummaryProps();
    }

    public void StartNew()
    {
        SelectedUsedCar = null;
        ClearUsedCarForm();
    }

    private async Task SaveUsedCarAsync()
    {
        if (NewUsedCarCarModelId is not int carModelId)
        {
            _setStatus("✗ Select a car model from the Cars tab list first.", false);
            return;
        }

        if (NewUsedCarSupplierId is not int supplierId)
        {
            _setStatus("✗ Select a supplier first.", false);
            return;
        }

        var selectedModel = CarModels.FirstOrDefault(model => model.Id == carModelId);
        if (selectedModel == null)
        {
            _setStatus("✗ The selected car model no longer exists.", false);
            return;
        }

        if (NewUsedCarLocationId is not int locationId)
        {
            _setStatus("✗ Select a location from the Locations tab list first.", false);
            return;
        }

        if (NewUsedCarIsReceived && NewUsedCarCustoms <= 0)
        {
            _setStatus("✗ Customs should be different than 0 when the car is marked as received.", false);
            return;
        }

        var request = new CreateUsedCarRequest
        {
            Barcode = string.IsNullOrWhiteSpace(NewUsedCarBarcode) ? null : NewUsedCarBarcode.Trim(),
            SupplierId = supplierId,
            CarModelId = carModelId,
            ModelYear = NewUsedCarModelYear,
            PriceCurrency = NormalizeCurrencyCode(NewUsedCarPriceCurrency) ?? _currencyRatesFeature.BaseCurrencyCode,
            Price = NewUsedCarPrice,
            LocationId = locationId,
            IsReceived = NewUsedCarIsReceived,
            IsShipped = NewUsedCarIsShipped,
            PartOut = NewUsedCarPartOut,
            Shipping = NewUsedCarShipping,
            Customs = NewUsedCarCustoms,
            Repairs = NewUsedCarRepairs,
            ExpectedSellThroughRate = NewUsedCarExpectedSellThroughRate
        };

        var result = await _coordinator.SaveUsedCarAsync(request, SelectedUsedCar);
        _setStatus(result.Message, result.Success);
        if (!result.Success)
        {
            return;
        }

        await _refreshAsync();
        StartNew();
    }

    private async Task RemoveSelectedUsedCarAsync()
    {
        var result = await _coordinator.DeleteUsedCarAsync(SelectedUsedCar);
        _setStatus(result.Message, result.Success);
        if (!result.Success)
        {
            return;
        }

        await _refreshAsync();
        StartNew();
    }

    private void OpenUsedCarGallery()
    {
        if (SelectedUsedCar is not { Id: > 0 } usedCar)
        {
            _notificationService.Show("Select a used car row first, then open the gallery.", "Gallery", NotificationKind.Warning);
            return;
        }

        _usedCarWorkspaceService.OpenGallery(CreateWorkspaceRequest(usedCar));
    }

    private void OpenUsedCarParts()
    {
        if (SelectedUsedCar is not { Id: > 0 } usedCar)
        {
            _notificationService.Show("Select a used car row first, then open its parts.", "Used Car Parts", NotificationKind.Warning);
            return;
        }

        _usedCarWorkspaceService.OpenParts(CreateWorkspaceRequest(usedCar));
        _ = _refreshAsync();
    }

    private void OpenUsedCarListingPackage()
    {
        if (SelectedUsedCar is not { Id: > 0 } usedCar)
        {
            _notificationService.Show("Select a used car row first, then generate its listing package.", "Listing Package", NotificationKind.Warning);
            return;
        }

        _usedCarWorkspaceService.OpenListingPackage(CreateWorkspaceRequest(usedCar));
    }

    private static UsedCarWorkspaceRequest CreateWorkspaceRequest(UsedCarEntry usedCar)
        => new()
        {
            UsedCarId = usedCar.Id,
            UsedCarName = usedCar.Car
        };

    private void ClearUsedCarForm()
    {
        NewUsedCarBarcode = string.Empty;
        NewUsedCarName = string.Empty;
        NewUsedCarModelYear = 0;
        NewUsedCarCarModelId = null;
        NewUsedCarSupplierId = null;
        NewUsedCarPriceCurrency = _currencyRatesFeature.BaseCurrencyCode;
        _newUsedCarPriceBase = 0m;
        _newUsedCarPriceCounter = 0m;
        _newUsedCarLocationId = null;
        _newUsedCarTransportation = 0m;
        _newUsedCarIsReceived = false;
        _newUsedCarIsShipped = false;
        _newUsedCarPartOut = 0m;
        _newUsedCarShipping = 0m;
        _newUsedCarCustoms = 0m;
        _newUsedCarRepairs = 0m;
        _newUsedCarTotalBeforeShipping = 0m;
        _newUsedCarGrandTotalBase = 0m;
        _newUsedCarGrandTotalCounter = 0m;
        _newUsedCarExpectedSellThroughRate = 0.80m;
        NewUsedCarPrice = 0m;
        RaiseEditorProps();
    }

    private void RefreshCurrencyCodes()
    {
        Replace(CurrencyCodes, _currencyRatesFeature.CurrencyCodes);

        if (!CurrencyCodes.Contains(NewUsedCarPriceCurrency, StringComparer.OrdinalIgnoreCase))
        {
            _newUsedCarPriceCurrency = _currencyRatesFeature.BaseCurrencyCode;
            OnPropertyChanged(nameof(NewUsedCarPriceCurrency));
        }

        RecalculateUsedCarPriceConversions();
        RaiseLabelProps();
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

        var locationCurrencyCode = NormalizeCurrencyCode(selectedLocation.ShippingFeesCurrencyCode) ?? _currencyRatesFeature.CounterCurrencyCode;
        NewUsedCarTransportation = _valuationService.ConvertAmountToCounterCurrency(
            _currencyRatesFeature.BuildContext(),
            selectedLocation.ShippingFees,
            locationCurrencyCode);
    }

    private void RecalculateUsedCarPriceConversions()
    {
        var valuation = _valuationService.Calculate(
            _currencyRatesFeature.BuildContext(),
            new UsedCarValuationInput
            {
                Price = NewUsedCarPrice,
                PriceCurrencyCode = NewUsedCarPriceCurrency,
                Transportation = NewUsedCarTransportation,
                PartOut = NewUsedCarPartOut,
                Shipping = NewUsedCarShipping,
                Customs = NewUsedCarCustoms,
                Repairs = NewUsedCarRepairs
            });

        NewUsedCarPriceBase = valuation.PriceBase;
        NewUsedCarPriceCounter = valuation.PriceCounter;
        NewUsedCarTotalBeforeShipping = valuation.TotalBeforeShipping;
        NewUsedCarGrandTotalBase = valuation.GrandTotalBase;
        NewUsedCarGrandTotalCounter = valuation.GrandTotalCounter;
    }

    private void RecalculateUsedCarTotals()
    {
        var valuation = _valuationService.Calculate(
            _currencyRatesFeature.BuildContext(),
            new UsedCarValuationInput
            {
                Price = NewUsedCarPrice,
                PriceCurrencyCode = NewUsedCarPriceCurrency,
                Transportation = NewUsedCarTransportation,
                PartOut = NewUsedCarPartOut,
                Shipping = NewUsedCarShipping,
                Customs = NewUsedCarCustoms,
                Repairs = NewUsedCarRepairs
            });

        NewUsedCarTotalBeforeShipping = valuation.TotalBeforeShipping;
        NewUsedCarGrandTotalBase = valuation.GrandTotalBase;
        NewUsedCarGrandTotalCounter = valuation.GrandTotalCounter;
    }

    private void RefreshUsedCarFilters()
    {
        var selectedId = SelectedUsedCar?.Id;
        var rows = UsedCars
            .Where(MatchesUsedCarSearch)
            .Where(MatchesUsedCarStatus)
            .OrderBy(entry => entry.IsWholesaleSold)
            .ThenByDescending(entry => entry.ProfitMapBreakEvenGapBase > 0m)
            .ThenByDescending(entry => entry.ProfitMapBreakEvenGapBase)
            .ThenBy(entry => entry.Car, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Replace(FilteredUsedCars, rows);

        if (selectedId.HasValue && FilteredUsedCars.All(entry => entry.Id != selectedId.Value))
        {
            SelectedUsedCar = FilteredUsedCars.FirstOrDefault();
        }
        else if (!selectedId.HasValue && SelectedUsedCar == null && FilteredUsedCars.Count > 0)
        {
            SelectedUsedCar = FilteredUsedCars[0];
        }

        RaiseDonorCommandProps();
    }

    private bool MatchesUsedCarSearch(UsedCarEntry entry)
    {
        if (string.IsNullOrWhiteSpace(UsedCarFilterText))
        {
            return true;
        }

        var haystack = string.Join(" ", new[]
        {
            entry.Barcode,
            entry.SupplierName,
            entry.Car,
            entry.ModelYear.ToString(System.Globalization.CultureInfo.InvariantCulture),
            entry.Location,
            entry.SaleStatusText,
            BuildUsedCarActionSummary(entry)
        });

        return haystack.Contains(UsedCarFilterText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private bool MatchesUsedCarStatus(UsedCarEntry entry)
    {
        return UsedCarStatusFilter switch
        {
            "Action needed" => !entry.IsWholesaleSold && (!entry.IsReceived
                || !entry.IsShipped
                || entry.FullCostBase <= 0m
                || entry.ProfitMapBreakEvenGapBase > 0m
                || entry.PartsRemovedCount <= 0),
            "Gap open" => entry.ProfitMapBreakEvenGapBase > 0m,
            "Needs part links" => !entry.IsWholesaleSold && entry.PartsRemovedCount <= 0,
            "Profit unlocked" => entry.FullCostBase > 0m && entry.ProfitMapBreakEvenGapBase <= 0m,
            "In transit" => !entry.IsReceived || !entry.IsShipped,
            "Wholesale sold" => entry.IsWholesaleSold,
            _ => true
        };
    }

    private static string BuildUsedCarActionSummary(UsedCarEntry entry)
    {
        if (entry.IsWholesaleSold)
        {
            return "Sold wholesale - review payment and margin.";
        }

        if (!entry.IsReceived)
        {
            return "Receive and cost - confirm arrival, customs, and landed cost.";
        }

        if (!entry.IsShipped)
        {
            return "Ship / clear - close shipping status before teardown.";
        }

        if (entry.FullCostBase <= 0m)
        {
            return "Confirm costs - add purchase and teardown costs before profit tracking.";
        }

        if (entry.ProfitMapBreakEvenGapBase <= 0m)
        {
            return "Profit unlocked - keep selling remaining donor stock.";
        }

        if (entry.PartsRemovedCount <= 0)
        {
            return "Link donor parts - assign harvested stock to this car.";
        }

        if (entry.RemainingStockValueBase >= entry.ProfitMapBreakEvenGapBase && entry.RemainingStockValueBase > 0m)
        {
            return "Sell stocked parts - current stock value can close break-even.";
        }

        return "Push removed parts - quote high-value donor parts first.";
    }

    private static string BuildUsedCarOperationalSummary(UsedCarEntry entry)
        => $"{entry.ProfitMapRecoveredPercentText} | {entry.PartsRemovedCount:N0} removed | {entry.RemainingStockQuantity:N0} in stock | {entry.ProfitMapBreakEvenStatus}";

    private string? NormalizeCurrencyCode(string? currencyCode)
        => _currencyRatesFeature.NormalizeCurrencyCode(currencyCode);

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

        RefreshUsedCarFilters();
        RaiseSummaryProps();
    }

    private void UsedCarEntryOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UsedCarEntry.GrandTotalBase)
            or nameof(UsedCarEntry.GrandTotalCounter)
            or nameof(UsedCarEntry.GrandTotalDisplay)
            or nameof(UsedCarEntry.NetProfitLossBase)
            or nameof(UsedCarEntry.NetProfitLossDisplay)
            or nameof(UsedCarEntry.IsReceived)
            or nameof(UsedCarEntry.IsShipped)
            or nameof(UsedCarEntry.IsWholesaleSold)
            or nameof(UsedCarEntry.PartsRemovedCount)
            or nameof(UsedCarEntry.RemainingStockValueBase)
            or nameof(UsedCarEntry.PartsSoldAmountBase))
        {
            RefreshUsedCarFilters();
            RaiseSummaryProps();
        }
    }

    private void CurrencyRatesFeatureOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CurrencyRatesManagementViewModel.BaseCurrencyCode)
            or nameof(CurrencyRatesManagementViewModel.CounterCurrencyCode)
            or nameof(CurrencyRatesManagementViewModel.DisplayCurrencyCode)
            or nameof(CurrencyRatesManagementViewModel.CurrencyRatesSummary))
        {
            RefreshCurrencyCodes();
            ApplyDisplayCurrencyToUsedCars();
        }
    }

    private static UsedCarEntry MapUsedCar(UsedCarDto usedCar)
    {
        return new UsedCarEntry
        {
            Id = usedCar.Id,
            Barcode = usedCar.Barcode,
            SupplierId = usedCar.SupplierId,
            SupplierName = usedCar.SupplierName,
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
            Repairs = usedCar.Repairs,
            TotalBeforeShipping = usedCar.TotalBeforeShipping,
            GrandTotalBase = usedCar.GrandTotalBase,
            GrandTotalCounter = usedCar.GrandTotalCounter,
            ExpectedSellThroughRate = usedCar.ExpectedSellThroughRate,
            PurchaseCostBase = usedCar.PurchaseCostBase,
            TransportationCostBase = usedCar.TransportationCostBase,
            CustomsCostBase = usedCar.CustomsCostBase,
            ShippingCostBase = usedCar.ShippingCostBase,
            PartOutCostBase = usedCar.PartOutCostBase,
            RepairsCostBase = usedCar.RepairsCostBase,
            FullCostBase = usedCar.FullCostBase,
            PartsRemovedCount = usedCar.PartsRemovedCount,
            PartsRemovedValueBase = usedCar.PartsRemovedValueBase,
            PartsSoldQuantity = usedCar.PartsSoldQuantity,
            PartsSoldAmountBase = usedCar.PartsSoldAmountBase,
            SalePriceBase = usedCar.SalePriceBase,
            IsWholesaleSold = usedCar.IsWholesaleSold,
            IsWholesaleForParts = usedCar.IsWholesaleForParts,
            WholesaleSaleNumber = usedCar.WholesaleSaleNumber,
            WholesalePaymentStatus = usedCar.WholesalePaymentStatus,
            RemainingStockQuantity = usedCar.RemainingStockQuantity,
            RemainingStockValueBase = usedCar.RemainingStockValueBase,
            NetProfitLossBase = usedCar.NetProfitLossBase
        };
    }

    private void RaiseEditorProps()
    {
        RaiseAll(
            nameof(NewUsedCarBarcode),
            nameof(NewUsedCarName),
            nameof(NewUsedCarModelYear),
            nameof(NewUsedCarCarModelId),
            nameof(NewUsedCarSupplierId),
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
            nameof(NewUsedCarRepairs),
            nameof(NewUsedCarTotalBeforeShipping),
            nameof(NewUsedCarGrandTotalBase),
            nameof(NewUsedCarGrandTotalCounter),
            nameof(NewUsedCarExpectedSellThroughRate));

        RaiseLabelProps();
    }

    private void RaiseLabelProps()
    {
        RaiseAll(
            nameof(UsedCarPriceLabel),
            nameof(UsedCarBasePriceLabel),
            nameof(UsedCarCounterPriceLabel),
            nameof(UsedCarTransportationLabel),
            nameof(UsedCarPartOutLabel),
            nameof(UsedCarShippingLabel),
            nameof(UsedCarCustomsLabel),
            nameof(UsedCarRepairsLabel),
            nameof(UsedCarTotalBeforeShippingLabel),
            nameof(UsedCarGrandTotalBaseLabel),
            nameof(UsedCarGrandTotalCounterLabel),
            nameof(UsedCarsTotalBaseLabel),
            nameof(UsedCarsTotalCounterLabel),
            nameof(UsedCarsTotalDisplayLabel),
            nameof(UsedCarsNetProfitLossDisplayLabel),
            nameof(UsedCarsBreakEvenGapDisplayLabel));
    }

    private void RaiseSummaryProps()
    {
        RaiseAll(
            nameof(UsedCarsTotalBaseAmount),
            nameof(UsedCarsTotalCounterAmount),
            nameof(UsedCarsNetProfitLossBaseAmount),
            nameof(UsedCarsTotalDisplayAmount),
            nameof(UsedCarsNetProfitLossDisplayAmount),
            nameof(UsedCarsBreakEvenGapDisplayAmount),
            nameof(UsedCarsReadyForTeardownCount),
            nameof(UsedCarsProfitUnlockedCount),
            nameof(UsedCarsBreakEvenRiskCount),
            nameof(UsedCarsNeedsPartLinksCount),
            nameof(FilteredUsedCarsCount));
    }

    private void RaiseDonorCommandProps()
    {
        RaiseAll(
            nameof(FilteredUsedCarsCount),
            nameof(UsedCarsReadyForTeardownCount),
            nameof(UsedCarsProfitUnlockedCount),
            nameof(UsedCarsBreakEvenRiskCount),
            nameof(UsedCarsNeedsPartLinksCount),
            nameof(UsedCarsBreakEvenGapDisplayAmount),
            nameof(SelectedUsedCarActionSummary),
            nameof(SelectedUsedCarOperationalSummary));
    }

    private void ApplyDisplayCurrencyToUsedCars()
    {
        var displayCurrencyCode = NormalizeCurrencyCode(_currencyRatesFeature.DisplayCurrencyCode)
            ?? _currencyRatesFeature.CounterCurrencyCode;
        var displayRateToBase = _currencyRatesFeature.ResolveRateToBaseCurrency(displayCurrencyCode);
        if (displayRateToBase <= 0m)
        {
            displayRateToBase = 1m;
        }

        foreach (var usedCar in UsedCars)
        {
            usedCar.DisplayCurrencyCode = displayCurrencyCode;
            usedCar.DisplayRateToBase = displayRateToBase;
        }

        RefreshUsedCarFilters();
        RaiseLabelProps();
        RaiseSummaryProps();
    }

    private void RaiseAll(params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        => target.ReplaceWith(source);

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
