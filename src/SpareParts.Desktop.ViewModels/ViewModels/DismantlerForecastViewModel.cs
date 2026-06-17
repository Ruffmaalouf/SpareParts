using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class DismantlerForecastViewModel : JsonListViewModelBase
    {
        private string _vehicleMake = string.Empty;
        private string? _vehicleModel;
        private string? _vehicleYear;
        private string _purchasePrice = string.Empty;
        private string _currency = string.Empty;
        private string _resultText = string.Empty;

        public DismantlerForecastViewModel(ICrudApiClient crudApi) : base(crudApi, "Forecast dismantler value.")
        {
            ForecastCommand = new RelayCommand(_ => ForecastAsync().SafeFireAndForget(HandleException));
        }

        public ICommand ForecastCommand { get; }

        public string VehicleMake
        {
            get => _vehicleMake;
            set { _vehicleMake = value; OnPropertyChanged(nameof(VehicleMake)); }
        }

        public string? VehicleModel
        {
            get => _vehicleModel;
            set { _vehicleModel = value; OnPropertyChanged(nameof(VehicleModel)); }
        }

        public string? VehicleYear
        {
            get => _vehicleYear;
            set { _vehicleYear = value; OnPropertyChanged(nameof(VehicleYear)); }
        }

        public string PurchasePrice
        {
            get => _purchasePrice;
            set { _purchasePrice = value; OnPropertyChanged(nameof(PurchasePrice)); }
        }

        public string Currency
        {
            get => _currency;
            set { _currency = value; OnPropertyChanged(nameof(Currency)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task ForecastAsync()
        {
            try
            {
                var payload = new
                {
                    vehicleMake = VehicleMake,
                    vehicleModel = VehicleModel,
                    vehicleYear = ParseIntOrNull(VehicleYear),
                    purchasePrice = ParseDecimalOrNull(PurchasePrice) ?? 0,
                    currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/dismantler-forecast", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Forecast ready.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
        private static decimal? ParseDecimalOrNull(string? value) => decimal.TryParse(value, out var result) ? result : null;
    }
}
