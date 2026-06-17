using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class WatchlistViewModel : JsonListViewModelBase
    {
        private string _partName = string.Empty;
        private string _vehicleMake = string.Empty;
        private string _vehicleModel = string.Empty;
        private string _vehicleYear = string.Empty;
        private string _maxBudget = string.Empty;
        private string _currency = string.Empty;
        private string _notes = string.Empty;

        public WatchlistViewModel(ICrudApiClient crudApi) : base(crudApi, "Load watchlist.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            RemoveCommand = new RelayCommand(_ => RemoveAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand RemoveCommand { get; }

        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
        }

        public string VehicleMake
        {
            get => _vehicleMake;
            set { _vehicleMake = value; OnPropertyChanged(nameof(VehicleMake)); }
        }

        public string VehicleModel
        {
            get => _vehicleModel;
            set { _vehicleModel = value; OnPropertyChanged(nameof(VehicleModel)); }
        }

        public string VehicleYear
        {
            get => _vehicleYear;
            set { _vehicleYear = value; OnPropertyChanged(nameof(VehicleYear)); }
        }

        public string MaxBudget
        {
            get => _maxBudget;
            set { _maxBudget = value; OnPropertyChanged(nameof(MaxBudget)); }
        }

        public string Currency
        {
            get => _currency;
            set { _currency = value; OnPropertyChanged(nameof(Currency)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/watchlist");
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} item(s) loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task CreateAsync()
        {
            try
            {
                var payload = new
                {
                    partName = PartName,
                    vehicleMake = VehicleMake,
                    vehicleModel = VehicleModel,
                    vehicleYear = ParseIntOrNull(VehicleYear),
                    maxBudget = ParseDecimalOrNull(MaxBudget),
                    currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency,
                    notes = Notes
                };
                await CrudApi.PostAsync("/api/watchlist", payload);
                Status = "Watchlist entry added.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartName = string.Empty;
                VehicleMake = string.Empty;
                VehicleModel = string.Empty;
                VehicleYear = string.Empty;
                MaxBudget = string.Empty;
                Currency = string.Empty;
                Notes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task RemoveAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a row first.";
                StatusBrush = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                await CrudApi.DeleteAsync($"/api/watchlist/{id}");
                Status = "Watchlist entry removed.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
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
