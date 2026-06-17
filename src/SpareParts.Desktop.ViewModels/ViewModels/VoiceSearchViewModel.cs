using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class VoiceSearchViewModel : JsonListViewModelBase
    {
        private string _transcript = string.Empty;
        private string _language = string.Empty;
        private string _hintVehicleMake = string.Empty;
        private string _resultText = string.Empty;

        public VoiceSearchViewModel(ICrudApiClient crudApi) : base(crudApi, "Search by voice transcript.")
        {
            SearchCommand = new RelayCommand(_ => SearchAsync().SafeFireAndForget(HandleException));
        }

        public ICommand SearchCommand { get; }

        public string Transcript
        {
            get => _transcript;
            set { _transcript = value; OnPropertyChanged(nameof(Transcript)); }
        }

        public string Language
        {
            get => _language;
            set { _language = value; OnPropertyChanged(nameof(Language)); }
        }

        public string HintVehicleMake
        {
            get => _hintVehicleMake;
            set { _hintVehicleMake = value; OnPropertyChanged(nameof(HintVehicleMake)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task SearchAsync()
        {
            IsLoading = true;
            Status = "Searching...";
            try
            {
                var payload = new
                {
                    transcript = Transcript,
                    language = string.IsNullOrWhiteSpace(Language) ? "en" : Language,
                    hintVehicleMake = HintVehicleMake
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/voice-search", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Voice search complete.";
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
    }
}
