using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class GarageStockViewModel : JsonListViewModelBase
    {
        private string _editingId = string.Empty;
        private string _name = string.Empty;
        private string _category = string.Empty;
        private string _barcode = string.Empty;
        private string _oemNumber = string.Empty;
        private string _quantity = string.Empty;
        private string _minimumQuantity = string.Empty;
        private string _notes = string.Empty;

        public GarageStockViewModel(ICrudApiClient crudApi) : base(crudApi, "Load garage stock.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            SaveCommand = new RelayCommand(_ => SaveAsync().SafeFireAndForget(HandleException));
            DeleteCommand = new RelayCommand(_ => DeleteAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }

        public string EditingId
        {
            get => _editingId;
            set { _editingId = value; OnPropertyChanged(nameof(EditingId)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string Barcode
        {
            get => _barcode;
            set { _barcode = value; OnPropertyChanged(nameof(Barcode)); }
        }

        public string OemNumber
        {
            get => _oemNumber;
            set { _oemNumber = value; OnPropertyChanged(nameof(OemNumber)); }
        }

        public string Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(nameof(Quantity)); }
        }

        public string MinimumQuantity
        {
            get => _minimumQuantity;
            set { _minimumQuantity = value; OnPropertyChanged(nameof(MinimumQuantity)); }
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
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/garage-stock");
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

        public async Task SaveAsync()
        {
            try
            {
                var payload = new
                {
                    name = Name,
                    category = Category,
                    barcode = Barcode,
                    oemNumber = OemNumber,
                    quantity = ParseIntOrNull(Quantity) ?? 0,
                    minimumQuantity = ParseIntOrNull(MinimumQuantity) ?? 0,
                    notes = Notes
                };

                if (string.IsNullOrEmpty(EditingId))
                {
                    await CrudApi.PostAsync("/api/garage-stock", payload);
                    Status = "Stock item created.";
                }
                else
                {
                    await CrudApi.PutAsync($"/api/garage-stock/{EditingId}", payload);
                    Status = "Stock item updated.";
                }

                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                EditingId = string.Empty;
                Name = string.Empty;
                Category = string.Empty;
                Barcode = string.Empty;
                OemNumber = string.Empty;
                Quantity = string.Empty;
                MinimumQuantity = string.Empty;
                Notes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task DeleteAsync()
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
                await CrudApi.DeleteAsync($"/api/garage-stock/{id}");
                Status = "Stock item deleted.";
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
