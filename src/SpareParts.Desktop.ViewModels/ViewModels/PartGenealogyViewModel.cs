using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartGenealogyViewModel : JsonListViewModelBase
    {
        private string _partId = string.Empty;
        private string _resultText = string.Empty;
        private string _eventType = string.Empty;
        private string? _description;
        private string? _actorName;

        public PartGenealogyViewModel(ICrudApiClient crudApi) : base(crudApi, "Load part genealogy.")
        {
            LoadGenealogyCommand = new RelayCommand(_ => LoadGenealogyAsync().SafeFireAndForget(HandleException));
            LogEventCommand = new RelayCommand(_ => LogEventAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadGenealogyCommand { get; }
        public ICommand LogEventCommand { get; }

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

        public string EventType
        {
            get => _eventType;
            set { _eventType = value; OnPropertyChanged(nameof(EventType)); }
        }

        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string? ActorName
        {
            get => _actorName;
            set { _actorName = value; OnPropertyChanged(nameof(ActorName)); }
        }

        public async Task LoadGenealogyAsync()
        {
            try
            {
                var data = await CrudApi.GetAsync<JsonElement>($"/api/part-genealogy/{PartId}");
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Genealogy loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task LogEventAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(PartId) ?? 0,
                    eventType = ParseIntOrNull(EventType) ?? 0,
                    description = Description,
                    actorName = ActorName
                };
                await CrudApi.PostAsync("/api/part-genealogy/event", payload);
                Status = "Event logged.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                EventType = string.Empty;
                Description = string.Empty;
                ActorName = string.Empty;
                if (!string.IsNullOrWhiteSpace(PartId))
                {
                    await LoadGenealogyAsync();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
