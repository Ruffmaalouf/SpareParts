using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class SymptomSearchViewModel : JsonListViewModelBase
    {
        private string _symptom = string.Empty;
        private string _vehicleMake = string.Empty;
        private string _vehicleModel = string.Empty;
        private string _vehicleYear = string.Empty;

        public SymptomSearchViewModel(ICrudApiClient crudApi) : base(crudApi, "Search by symptom.")
        {
            SearchCommand = new RelayCommand(_ => SearchAsync().SafeFireAndForget(HandleException));
        }

        public ICommand SearchCommand { get; }

        public string Symptom
        {
            get => _symptom;
            set { _symptom = value; OnPropertyChanged(nameof(Symptom)); }
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

        public async Task SearchAsync()
        {
            IsLoading = true;
            Status = "Searching...";
            try
            {
                var payload = new
                {
                    symptom = Symptom,
                    vehicleMake = VehicleMake,
                    vehicleModel = VehicleModel,
                    vehicleYear = ParseIntOrNull(VehicleYear)
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/symptom-search", payload);
                var arrayElement = data.ValueKind == JsonValueKind.Array ? data : data.GetProperty("results");
                var items = JsonSerializer.Deserialize<List<JsonElement>>(arrayElement.GetRawText()) ?? new List<JsonElement>();
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} suggestion(s) found.";
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

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
