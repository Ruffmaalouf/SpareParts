using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class NeedboardViewModel : JsonListViewModelBase
    {
        private string _buyerName = string.Empty;
        private string _buyerPhone = string.Empty;
        private string _partName = string.Empty;
        private string _vehicleMake = string.Empty;
        private string _vehicleModel = string.Empty;
        private string _vehicleYear = string.Empty;
        private string _budget = string.Empty;
        private string _notes = string.Empty;
        private string _locationCity = string.Empty;
        private string _urgency = string.Empty;

        public NeedboardViewModel(ICrudApiClient crudApi) : base(crudApi, "Load needboard.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            CancelCommand = new RelayCommand(_ => CancelAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand CancelCommand { get; }

        public string BuyerName
        {
            get => _buyerName;
            set { _buyerName = value; OnPropertyChanged(nameof(BuyerName)); }
        }

        public string BuyerPhone
        {
            get => _buyerPhone;
            set { _buyerPhone = value; OnPropertyChanged(nameof(BuyerPhone)); }
        }

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

        public string Budget
        {
            get => _budget;
            set { _budget = value; OnPropertyChanged(nameof(Budget)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string LocationCity
        {
            get => _locationCity;
            set { _locationCity = value; OnPropertyChanged(nameof(LocationCity)); }
        }

        public string Urgency
        {
            get => _urgency;
            set { _urgency = value; OnPropertyChanged(nameof(Urgency)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/needboard");
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
                    buyerName = BuyerName,
                    buyerPhone = BuyerPhone,
                    partName = PartName,
                    vehicleMake = VehicleMake,
                    vehicleModel = VehicleModel,
                    vehicleYear = ParseIntOrNull(VehicleYear),
                    budget = ParseDecimalOrNull(Budget),
                    notes = Notes,
                    locationCity = LocationCity,
                    urgency = Urgency
                };
                await CrudApi.PostAsync("/api/needboard", payload);
                Status = "Need posted.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                BuyerName = string.Empty;
                BuyerPhone = string.Empty;
                PartName = string.Empty;
                VehicleMake = string.Empty;
                VehicleModel = string.Empty;
                VehicleYear = string.Empty;
                Budget = string.Empty;
                Notes = string.Empty;
                LocationCity = string.Empty;
                Urgency = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task CancelAsync()
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
                await CrudApi.PutAsync($"/api/needboard/{id}/cancel", new { });
                Status = "Need cancelled.";
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
