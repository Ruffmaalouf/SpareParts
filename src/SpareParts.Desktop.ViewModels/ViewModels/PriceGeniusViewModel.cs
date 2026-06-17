using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PriceGeniusViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _resultText = string.Empty;

        public PriceGeniusViewModel(ICrudApiClient crudApi) : base(crudApi, "Suggest a price.")
        {
            SuggestCommand = new RelayCommand(_ => SuggestAsync().SafeFireAndForget(HandleException));
        }

        public ICommand SuggestCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task SuggestAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/price-genius/suggest", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Suggestion ready.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
