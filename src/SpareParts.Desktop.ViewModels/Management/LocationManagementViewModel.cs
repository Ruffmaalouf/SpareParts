using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class LocationManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private LocationDto? _selectedLocation;
        private string _newLocationName = string.Empty;
        private decimal _newLocationShippingFees;
        private string _newLocationShippingFeesCurrencyCode = "USD";

        public LocationManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCommand            = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand          = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand        = new RelayCommand(_ => StartNew());
            RefreshCommand         = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.Location"));
        }

        public ObservableCollection<LocationDto> Locations { get; } = new();
        public ObservableCollection<string> CurrencyCodes { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

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

        public void StartNew() => ClearForm(_ctx.GetDefaultCurrencyCode());

        private async Task SaveAsync()
        {
            var result = await _ctx.Coordinator.SaveLocationAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeleteLocationAsync(SelectedLocation);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }
    }
}
