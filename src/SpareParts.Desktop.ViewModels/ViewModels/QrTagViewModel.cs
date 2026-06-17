using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class QrTagViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _resultText = string.Empty;

        public QrTagViewModel(ICrudApiClient crudApi) : base(crudApi, "Look up a QR tag.")
        {
            LookupCommand = new RelayCommand(_ => LookupAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LookupCommand { get; }

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

        public async Task LookupAsync()
        {
            try
            {
                var data = await CrudApi.GetAsync<JsonElement>($"/api/qr-tag/{PartId}");
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Lookup complete.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
