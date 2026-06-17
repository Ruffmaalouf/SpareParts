using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartReserveViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _partName = string.Empty;
        private string _reservedByName = string.Empty;
        private string _reservedByPhone = string.Empty;
        private string _holdHours = string.Empty;
        private string _notes = string.Empty;

        public PartReserveViewModel(ICrudApiClient crudApi) : base(crudApi, "Load part reservations.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            ReleaseCommand = new RelayCommand(_ => ReleaseAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand ReleaseCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
        }

        public string ReservedByName
        {
            get => _reservedByName;
            set { _reservedByName = value; OnPropertyChanged(nameof(ReservedByName)); }
        }

        public string ReservedByPhone
        {
            get => _reservedByPhone;
            set { _reservedByPhone = value; OnPropertyChanged(nameof(ReservedByPhone)); }
        }

        public string HoldHours
        {
            get => _holdHours;
            set { _holdHours = value; OnPropertyChanged(nameof(HoldHours)); }
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
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/part-reservations");
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
                    partId = PartId,
                    partName = PartName,
                    reservedByName = ReservedByName,
                    reservedByPhone = ReservedByPhone,
                    holdHours = ParseIntOrNull(HoldHours) ?? 24,
                    notes = Notes
                };
                await CrudApi.PostAsync("/api/part-reservations", payload);
                Status = "Reservation created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartId = string.Empty;
                PartName = string.Empty;
                ReservedByName = string.Empty;
                ReservedByPhone = string.Empty;
                HoldHours = string.Empty;
                Notes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task ReleaseAsync()
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
                await CrudApi.PutAsync($"/api/part-reservations/{id}/release", new { });
                Status = "Reservation released.";
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
