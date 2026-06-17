using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ConditionScannerViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _photoUrls = string.Empty;
        private string? _notes;
        private string _resultText = string.Empty;

        public ConditionScannerViewModel(ICrudApiClient crudApi) : base(crudApi, "Scan part condition.")
        {
            ScanCommand = new RelayCommand(_ => ScanAsync().SafeFireAndForget(HandleException));
        }

        public ICommand ScanCommand { get; }

        public string PartId
        {
            get => _partId;
            set { _partId = value; OnPropertyChanged(nameof(PartId)); }
        }

        public string PhotoUrls
        {
            get => _photoUrls;
            set { _photoUrls = value; OnPropertyChanged(nameof(PhotoUrls)); }
        }

        public string? Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(nameof(Notes)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task ScanAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    photoUrls = SplitCsv(PhotoUrls),
                    notes = Notes
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/condition-scanner/scan", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Scan complete.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static List<string> SplitCsv(string? value) => string.IsNullOrWhiteSpace(value) ? new List<string>() : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
