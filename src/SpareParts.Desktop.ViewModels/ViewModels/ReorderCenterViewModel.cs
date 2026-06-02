using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Forecasting;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReorderCenterViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isLoading;
        private string _status = "Load reorder suggestions.";
        private Brush _statusBrush = Brushes.LightGray;

        public ReorderCenterViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<ReorderSuggestionDto> Items { get; } = new();
        public ICommand LoadCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading reorder suggestions...";
            StatusBrush = Brushes.LightGray;
            try
            {
                var items = await _crudApi.GetAllAsync<ReorderSuggestionDto>("api/reorder/suggestions");
                Items.Clear();
                foreach (var item in items) Items.Add(item);
                Status = $"{items.Count} suggestion(s) loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                Status = ex.Message;
                StatusBrush = Brushes.OrangeRed;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void HandleBackgroundException(Exception ex) { Status = ex.Message; StatusBrush = Brushes.OrangeRed; IsLoading = false; }
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
