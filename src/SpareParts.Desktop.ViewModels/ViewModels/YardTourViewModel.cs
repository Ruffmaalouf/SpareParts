using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class YardTourViewModel : JsonListViewModelBase
    {
        private string _title = string.Empty;
        private string? _description;
        private string? _scheduledAt;
        private string? _streamUrl;
        private string _newStatus = string.Empty;

        public YardTourViewModel(ICrudApiClient crudApi) : base(crudApi, "Load yard tours.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            UpdateStatusCommand = new RelayCommand(_ => UpdateStatusAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand UpdateStatusCommand { get; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string? Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string? ScheduledAt
        {
            get => _scheduledAt;
            set { _scheduledAt = value; OnPropertyChanged(nameof(ScheduledAt)); }
        }

        public string? StreamUrl
        {
            get => _streamUrl;
            set { _streamUrl = value; OnPropertyChanged(nameof(StreamUrl)); }
        }

        public string NewStatus
        {
            get => _newStatus;
            set { _newStatus = value; OnPropertyChanged(nameof(NewStatus)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/yard-tours");
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
                    title = Title,
                    description = Description,
                    scheduledAt = ScheduledAt,
                    streamUrl = StreamUrl
                };
                await CrudApi.PostAsync("/api/yard-tours", payload);
                Status = "Yard tour created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                Title = string.Empty;
                Description = string.Empty;
                ScheduledAt = string.Empty;
                StreamUrl = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task UpdateStatusAsync()
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
                await CrudApi.PutAsync($"/api/yard-tours/{id}/status", NewStatus);
                Status = "Yard tour status updated.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
