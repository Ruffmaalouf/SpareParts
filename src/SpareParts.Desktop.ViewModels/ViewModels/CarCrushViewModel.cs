using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CarCrushViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isBusy;
        private string _status = "Enter vehicle details and generate a checklist.";
        private Brush _statusBrush = Brushes.LightGray;
        private string _vin = string.Empty;
        private string _make = string.Empty;
        private string _model = string.Empty;
        private string _year = string.Empty;
        private string _trim = string.Empty;
        private string _vehicleDescription = string.Empty;
        private int? _createdCount;

        public CarCrushViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            DecodeVinCommand = new RelayCommand(_ => DecodeVinAsync().SafeFireAndForget(HandleException));
            GenerateChecklistCommand = new RelayCommand(_ => GenerateChecklistAsync().SafeFireAndForget(HandleException));
            CreateListingsCommand = new RelayCommand(_ => CreateListingsAsync().SafeFireAndForget(HandleException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<CarCrushPartRow> Checklist { get; } = new();
        public ICommand DecodeVinCommand { get; }
        public ICommand GenerateChecklistCommand { get; }
        public ICommand CreateListingsCommand { get; }

        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public string Vin
        {
            get => _vin;
            set { _vin = value; OnPropertyChanged(nameof(Vin)); }
        }

        public string Make
        {
            get => _make;
            set { _make = value; OnPropertyChanged(nameof(Make)); }
        }

        public string Model
        {
            get => _model;
            set { _model = value; OnPropertyChanged(nameof(Model)); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(nameof(Year)); }
        }

        public string Trim
        {
            get => _trim;
            set { _trim = value; OnPropertyChanged(nameof(Trim)); }
        }

        public string VehicleDescription
        {
            get => _vehicleDescription;
            private set { _vehicleDescription = value; OnPropertyChanged(nameof(VehicleDescription)); }
        }

        public int? CreatedCount
        {
            get => _createdCount;
            private set { _createdCount = value; OnPropertyChanged(nameof(CreatedCount)); }
        }

        public async Task DecodeVinAsync()
        {
            if (string.IsNullOrWhiteSpace(Vin))
            {
                Status = "Enter a VIN to decode.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Decoding VIN...";
            try
            {
                var result = await _crudApi.GetAsync<JsonElement>($"/api/vehicle-profile/vin-decode?vin={Uri.EscapeDataString(Vin)}");
                if (result.TryGetProperty("success", out var successProp) && successProp.ValueKind == JsonValueKind.True)
                {
                    if (result.TryGetProperty("make", out var makeProp) && makeProp.ValueKind == JsonValueKind.String)
                        Make = makeProp.GetString() ?? Make;
                    if (result.TryGetProperty("model", out var modelProp) && modelProp.ValueKind == JsonValueKind.String)
                        Model = modelProp.GetString() ?? Model;
                    if (result.TryGetProperty("year", out var yearProp) && yearProp.ValueKind == JsonValueKind.Number)
                        Year = yearProp.ToString();
                    if (result.TryGetProperty("trim", out var trimProp) && trimProp.ValueKind == JsonValueKind.String)
                        Trim = trimProp.GetString() ?? Trim;
                    Status = "VIN decoded.";
                    StatusBrush = Brushes.LightGreen;
                }
                else
                {
                    var message = result.TryGetProperty("errorMessage", out var errorProp) ? errorProp.GetString() : "VIN decode failed.";
                    Status = message ?? "VIN decode failed.";
                    StatusBrush = Brushes.OrangeRed;
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task GenerateChecklistAsync()
        {
            if (string.IsNullOrWhiteSpace(Make) || string.IsNullOrWhiteSpace(Model))
            {
                Status = "Make and model are required.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Generating parts checklist...";
            Checklist.Clear();
            CreatedCount = null;
            try
            {
                var payload = new
                {
                    vin = string.IsNullOrWhiteSpace(Vin) ? null : Vin,
                    make = Make,
                    model = Model,
                    year = ParseIntOrNull(Year),
                    trim = string.IsNullOrWhiteSpace(Trim) ? null : Trim
                };
                var result = await _crudApi.PostAsync<JsonElement>("/api/car-crush/checklist", payload);

                JsonElement parts = default;
                if (result.ValueKind == JsonValueKind.Array)
                {
                    parts = result;
                }
                else if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("parts", out var partsProp) && partsProp.ValueKind == JsonValueKind.Array)
                {
                    parts = partsProp;
                }

                if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("vehicleDescription", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                {
                    VehicleDescription = descProp.GetString() ?? string.Empty;
                }
                else
                {
                    VehicleDescription = string.Join(" ", new[] { Year, Make, Model, Trim }.Where(value => !string.IsNullOrWhiteSpace(value)));
                }

                if (parts.ValueKind == JsonValueKind.Array)
                {
                    foreach (var part in parts.EnumerateArray())
                    {
                        Checklist.Add(new CarCrushPartRow
                        {
                            PartName = GetString(part, "partName") ?? GetString(part, "PartName"),
                            Category = GetString(part, "category") ?? GetString(part, "Category"),
                            OemHint = GetString(part, "oemHint") ?? GetString(part, "OemHint"),
                            IsHighValue = GetBool(part, "isHighValue") || GetBool(part, "IsHighValue"),
                            IsFastMoving = GetBool(part, "isFastMoving") || GetBool(part, "IsFastMoving"),
                            Selected = true
                        });
                    }
                }

                Status = Checklist.Count == 0 ? "No parts generated. Try again." : string.Empty;
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task CreateListingsAsync()
        {
            var selected = Checklist.Where(part => part.Selected).ToList();
            if (selected.Count == 0)
            {
                Status = "Select at least one part to create listings.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsBusy = true;
            Status = "Creating draft listings...";
            try
            {
                var payload = new
                {
                    vin = string.IsNullOrWhiteSpace(Vin) ? null : Vin,
                    make = Make,
                    model = Model,
                    year = ParseIntOrNull(Year),
                    trim = string.IsNullOrWhiteSpace(Trim) ? null : Trim,
                    parts = selected.Select(part => new { partName = part.PartName, category = part.Category, oemHint = part.OemHint }).ToList()
                };
                var result = await _crudApi.PostAsync<JsonElement>("/api/car-crush/create-listings", payload);
                CreatedCount = result.ValueKind == JsonValueKind.Object && result.TryGetProperty("createdCount", out var countProp) && countProp.ValueKind == JsonValueKind.Number
                    ? countProp.GetInt32()
                    : selected.Count;
                Status = string.Empty;
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void HandleException(Exception ex)
        {
            Status = ex.Message;
            StatusBrush = Brushes.OrangeRed;
            IsBusy = false;
        }

        private static string? GetString(JsonElement element, string propertyName) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
                ? prop.GetString()
                : null;

        private static bool GetBool(JsonElement element, string propertyName) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out var prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False) && prop.GetBoolean();

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
