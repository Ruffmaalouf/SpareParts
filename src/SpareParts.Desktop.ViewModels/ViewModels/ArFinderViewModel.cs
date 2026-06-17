using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ArFinderViewModel : JsonListViewModelBase
    {
        private string _photoUrl = string.Empty;
        private string? _make;
        private string? _model;
        private string? _year;
        private string _resultText = string.Empty;

        public ArFinderViewModel(ICrudApiClient crudApi) : base(crudApi, "Scan a photo to find parts.")
        {
            ScanCommand = new RelayCommand(_ => ScanAsync().SafeFireAndForget(HandleException));
        }

        public ICommand ScanCommand { get; }

        public string PhotoUrl
        {
            get => _photoUrl;
            set { _photoUrl = value; OnPropertyChanged(nameof(PhotoUrl)); }
        }

        public string? Make
        {
            get => _make;
            set { _make = value; OnPropertyChanged(nameof(Make)); }
        }

        public string? Model
        {
            get => _model;
            set { _model = value; OnPropertyChanged(nameof(Model)); }
        }

        public string? Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(nameof(Year)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task ScanAsync()
        {
            IsLoading = true;
            Status = "Scanning...";
            try
            {
                var payload = new
                {
                    photoUrl = PhotoUrl,
                    make = Make,
                    model = Model,
                    year = ParseIntOrNull(Year)
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/ar-finder/scan", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Scan complete.";
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

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
