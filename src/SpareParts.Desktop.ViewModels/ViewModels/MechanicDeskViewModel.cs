using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class MechanicDeskViewModel : JsonListViewModelBase
    {
        private string _customerName = string.Empty;
        private string _customerPhone = string.Empty;
        private string _vehicleMake = string.Empty;
        private string _vehicleModel = string.Empty;
        private string _vehicleYear = string.Empty;
        private string _vehicleVin = string.Empty;
        private string _notes = string.Empty;
        private string _deadline = string.Empty;

        public MechanicDeskViewModel(ICrudApiClient crudApi) : base(crudApi, "Load mechanic desk.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            SendCommand = new RelayCommand(_ => SendAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand SendCommand { get; }

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(nameof(CustomerName)); }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set { _customerPhone = value; OnPropertyChanged(nameof(CustomerPhone)); }
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

        public string VehicleVin
        {
            get => _vehicleVin;
            set { _vehicleVin = value; OnPropertyChanged(nameof(VehicleVin)); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string Deadline
        {
            get => _deadline;
            set { _deadline = value; OnPropertyChanged(nameof(Deadline)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/mechanic-desk");
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
                    customerName = CustomerName,
                    customerPhone = CustomerPhone,
                    vehicleMake = VehicleMake,
                    vehicleModel = VehicleModel,
                    vehicleYear = ParseIntOrNull(VehicleYear),
                    vehicleVin = VehicleVin,
                    notes = Notes,
                    deadline = string.IsNullOrWhiteSpace(Deadline) ? null : Deadline
                };
                await CrudApi.PostAsync("/api/mechanic-desk", payload);
                Status = "Job created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                CustomerName = string.Empty;
                CustomerPhone = string.Empty;
                VehicleMake = string.Empty;
                VehicleModel = string.Empty;
                VehicleYear = string.Empty;
                VehicleVin = string.Empty;
                Notes = string.Empty;
                Deadline = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task SendAsync()
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
                await CrudApi.PutAsync($"/api/mechanic-desk/{id}/send", new { });
                Status = "Job sent.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
