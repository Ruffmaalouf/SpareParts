using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CarTwinViewModel : JsonListViewModelBase
    {
        private DataTable _timelineTable = new();
        private DataTable _partsTable = new();
        private string _usedCarId = string.Empty;
        private string _eventType = "Note";
        private string _mileage = string.Empty;
        private string _condition = string.Empty;
        private string _location = string.Empty;
        private string _note = string.Empty;
        private string _resultText = string.Empty;

        public CarTwinViewModel(ICrudApiClient crudApi) : base(crudApi, "Track a vehicle's real-world condition, installed parts, and history.")
        {
            LoadCarsCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            LoadTwinCommand = new RelayCommand(_ => LoadTwinAsync().SafeFireAndForget(HandleException));
            AddEventCommand = new RelayCommand(_ => AddEventAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCarsCommand { get; }
        public ICommand LoadTwinCommand { get; }
        public ICommand AddEventCommand { get; }

        public DataTable TimelineTable
        {
            get => _timelineTable;
            private set { _timelineTable = value; OnPropertyChanged(nameof(TimelineTable)); }
        }

        public DataTable PartsTable
        {
            get => _partsTable;
            private set { _partsTable = value; OnPropertyChanged(nameof(PartsTable)); }
        }

        public string UsedCarId
        {
            get => _usedCarId;
            set { _usedCarId = value; OnPropertyChanged(nameof(UsedCarId)); }
        }

        public string EventType
        {
            get => _eventType;
            set { _eventType = value; OnPropertyChanged(nameof(EventType)); }
        }

        public string Mileage
        {
            get => _mileage;
            set { _mileage = value; OnPropertyChanged(nameof(Mileage)); }
        }

        public string Condition
        {
            get => _condition;
            set { _condition = value; OnPropertyChanged(nameof(Condition)); }
        }

        public string Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        public string Note
        {
            get => _note;
            set { _note = value; OnPropertyChanged(nameof(Note)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/usedcars");
                Table = JsonGridHelper.ToDataTable(items);
                if (string.IsNullOrWhiteSpace(UsedCarId))
                {
                    var selectedId = GetSelectedId() ?? TryReadFirstId(items);
                    UsedCarId = selectedId?.ToString() ?? string.Empty;
                }

                Status = $"Loaded {items.Count} vehicles.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadTwinAsync()
        {
            var usedCarId = ResolveUsedCarId();
            if (!usedCarId.HasValue)
            {
                Status = "Select a vehicle or enter a Used Car ID first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsLoading = true;
            try
            {
                var twin = await CrudApi.GetAsync<JsonElement>($"/api/usedcars/{usedCarId.Value}/twin");
                PartsTable = JsonGridHelper.ToDataTable(ReadArray(twin, "parts"));
                TimelineTable = JsonGridHelper.ToDataTable(ReadArray(twin, "timeline"));
                ResultText = JsonSerializer.Serialize(twin, new JsonSerializerOptions { WriteIndented = true });
                UsedCarId = usedCarId.Value.ToString();
                Status = "Digital twin loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task AddEventAsync()
        {
            var usedCarId = ResolveUsedCarId();
            if (!usedCarId.HasValue)
            {
                Status = "Select a vehicle or enter a Used Car ID first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            IsLoading = true;
            try
            {
                var payload = new
                {
                    eventType = string.IsNullOrWhiteSpace(EventType) ? "Note" : EventType,
                    mileage = ParseIntOrNull(Mileage),
                    condition = string.IsNullOrWhiteSpace(Condition) ? null : Condition,
                    location = string.IsNullOrWhiteSpace(Location) ? null : Location,
                    note = string.IsNullOrWhiteSpace(Note) ? null : Note
                };
                await CrudApi.PostAsync($"/api/usedcars/{usedCarId.Value}/state-events", payload);
                Mileage = string.Empty;
                Condition = string.Empty;
                Location = string.Empty;
                Note = string.Empty;
                Status = "State event recorded.";
                StatusBrush = Brushes.LightGreen;
                await LoadTwinAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private long? ResolveUsedCarId()
        {
            if (long.TryParse(UsedCarId, out var explicitId))
            {
                return explicitId;
            }

            return GetSelectedId();
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;

        private static long? TryReadFirstId(IReadOnlyList<JsonElement> items)
        {
            foreach (var item in items)
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (item.TryGetProperty("id", out var idElement) && idElement.TryGetInt64(out var id))
                {
                    return id;
                }
            }

            return null;
        }

        private static List<JsonElement> ReadArray(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            {
                return new List<JsonElement>();
            }

            var rows = new List<JsonElement>();
            foreach (var row in property.EnumerateArray())
            {
                rows.Add(row);
            }

            return rows;
        }
    }
}
