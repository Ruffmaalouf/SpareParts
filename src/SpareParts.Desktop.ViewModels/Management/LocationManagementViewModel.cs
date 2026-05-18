using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class LocationManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private Func<string>? _getDefaultCurrencyCode;
        private LocationDto? _selectedLocation;
        private string _newLocationName = string.Empty;
        private decimal _newLocationShippingFees;
        private string _newLocationShippingFeesCurrencyCode = "USD";

        public ObservableCollection<LocationDto> Locations { get; } = new();
        public ObservableCollection<string> CurrencyCodes { get; } = new();
        public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportFromExcelCommand { get; private set; } = new RelayCommand(_ => { });

        public void Configure(
            ManagementCoordinator coordinator,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            Func<string> getDefaultCurrencyCode,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            _getDefaultCurrencyCode = getDefaultCurrencyCode;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.Location"));
        }

        public void LoadCurrencyCodes(IEnumerable<string> currencyCodes)
        {
            CurrencyCodes.Clear();
            foreach (var code in currencyCodes)
            {
                CurrencyCodes.Add(code);
            }
        }

        public LocationDto? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (!SetProperty(ref _selectedLocation, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

        public string NewLocationName
        {
            get => _newLocationName;
            set => SetProperty(ref _newLocationName, value);
        }

        public decimal NewLocationShippingFees
        {
            get => _newLocationShippingFees;
            set => SetProperty(ref _newLocationShippingFees, value);
        }

        public string NewLocationShippingFeesCurrencyCode
        {
            get => _newLocationShippingFeesCurrencyCode;
            set => SetProperty(ref _newLocationShippingFeesCurrencyCode, value);
        }

        public void PopulateForm(LocationDto location)
        {
            NewLocationName = location.Name;
            NewLocationShippingFees = location.ShippingFees;
            NewLocationShippingFeesCurrencyCode = string.IsNullOrWhiteSpace(location.ShippingFeesCurrencyCode)
                ? "USD"
                : location.ShippingFeesCurrencyCode.Trim().ToUpperInvariant();
        }

        public void ClearForm(string defaultCurrencyCode)
        {
            NewLocationName = string.Empty;
            NewLocationShippingFees = 0m;
            NewLocationShippingFeesCurrencyCode = string.IsNullOrWhiteSpace(defaultCurrencyCode)
                ? "USD"
                : defaultCurrencyCode.Trim().ToUpperInvariant();
            SelectedLocation = null;
        }

        public void StartNew() => ClearForm(_getDefaultCurrencyCode?.Invoke() ?? "USD");

        private async Task SaveAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveLocationAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteLocationAsync(SelectedLocation);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }
    }
}
