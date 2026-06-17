using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class LiveInspectionViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _buyerName = string.Empty;
        private string? _buyerPhone;
        private string? _notes;

        public LiveInspectionViewModel(ICrudApiClient crudApi) : base(crudApi, "Load live inspections.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string BuyerName
        {
            get => _buyerName;
            set { _buyerName = value; OnPropertyChanged(nameof(BuyerName)); }
        }

        public string? BuyerPhone
        {
            get => _buyerPhone;
            set { _buyerPhone = value; OnPropertyChanged(nameof(BuyerPhone)); }
        }

        public string? Notes
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
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/live-inspection");
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
                    partId = ParseIntOrNull(PartId) ?? 0,
                    buyerName = BuyerName,
                    buyerPhone = BuyerPhone,
                    notes = Notes
                };
                await CrudApi.PostAsync("/api/live-inspection", payload);
                Status = "Live inspection created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartId = string.Empty;
                BuyerName = string.Empty;
                BuyerPhone = string.Empty;
                Notes = string.Empty;
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
