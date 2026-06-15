using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CustomerAgingViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isLoading;
        private string _status = "Load customer aging.";
        private Brush _statusBrush = Brushes.LightGray;

        public CustomerAgingViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<CustomerAgingDto> Items { get; } = new();
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
            Status = "Loading customer aging...";
            StatusBrush = Brushes.LightGray;
            try
            {
                var items = await _crudApi.GetAllAsync<CustomerAgingDto>("api/customers/aging");
                Items.Clear();
                foreach (var item in items) Items.Add(item);
                Status = $"{items.Count} customer balance row(s) loaded.";
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
