using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class NewVsUsedViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string? _oemNewPrice;
        private string? _aftermarketNewPrice;
        private string _resultText = string.Empty;

        public NewVsUsedViewModel(ICrudApiClient crudApi) : base(crudApi, "Load new vs used pricing.")
        {
            LoadDataCommand = new RelayCommand(_ => LoadDataAsync().SafeFireAndForget(HandleException));
            UpdatePricesCommand = new RelayCommand(_ => UpdatePricesAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadDataCommand { get; }
        public ICommand UpdatePricesCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string? OemNewPrice
        {
            get => _oemNewPrice;
            set { _oemNewPrice = value; OnPropertyChanged(nameof(OemNewPrice)); }
        }

        public string? AftermarketNewPrice
        {
            get => _aftermarketNewPrice;
            set { _aftermarketNewPrice = value; OnPropertyChanged(nameof(AftermarketNewPrice)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var data = await CrudApi.GetAsync<JsonElement>($"/api/new-vs-used/{PartId}");
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Data loaded.";
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

        public async Task UpdatePricesAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    oemNewPrice = ParseDecimalOrNull(OemNewPrice),
                    aftermarketNewPrice = ParseDecimalOrNull(AftermarketNewPrice)
                };
                await CrudApi.PutAsync("/api/new-vs-used/prices", payload);
                Status = "Prices updated.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadDataAsync();
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
