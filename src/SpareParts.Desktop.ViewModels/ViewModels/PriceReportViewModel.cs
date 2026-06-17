using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PriceReportViewModel : JsonListViewModelBase
    {
        private string? _partName;
        private string? _make;
        private string? _model;
        private string? _year;
        private string _resultText = string.Empty;

        public PriceReportViewModel(ICrudApiClient crudApi) : base(crudApi, "Generate a price report.")
        {
            GenerateCommand = new RelayCommand(_ => GenerateAsync().SafeFireAndForget(HandleException));
        }

        public ICommand GenerateCommand { get; }

        public string? PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
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

        public async Task GenerateAsync()
        {
            IsLoading = true;
            Status = "Generating...";
            try
            {
                var payload = new
                {
                    partName = PartName,
                    make = Make,
                    model = Model,
                    year = ParseIntOrNull(Year)
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/price-report", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Price report generated.";
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
