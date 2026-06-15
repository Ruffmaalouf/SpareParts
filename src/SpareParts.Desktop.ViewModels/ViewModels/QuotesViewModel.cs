using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class QuotesViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private bool _isLoading;
        private string _status = "Load quotes.";
        private Brush _statusBrush = Brushes.LightGray;
        private string _statusFilter = "Draft";
        private string _searchText = string.Empty;
        private QuoteDetailsDto? _selectedQuote;
        private QuoteLookupDto? _selectedItem;

        public QuotesViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            OpenSelectedCommand = new RelayCommand(_ => OpenSelectedAsync().SafeFireAndForget(HandleBackgroundException));
            MarkSentCommand = new RelayCommand(_ => UpdateSelectedStatusAsync("Sent").SafeFireAndForget(HandleBackgroundException));
            MarkAcceptedCommand = new RelayCommand(_ => UpdateSelectedStatusAsync("Accepted").SafeFireAndForget(HandleBackgroundException));
            MarkDeclinedCommand = new RelayCommand(_ => UpdateSelectedStatusAsync("Declined").SafeFireAndForget(HandleBackgroundException));
            ConvertSelectedCommand = new RelayCommand(_ => ConvertSelectedAsync().SafeFireAndForget(HandleBackgroundException));
            DeleteSelectedCommand = new RelayCommand(_ => DeleteSelectedAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<QuoteLookupDto> Items { get; } = new();

        public ObservableCollection<string> StatusFilters { get; } = new(new[]
        {
            "Draft",
            "Sent",
            "Accepted",
            "Declined",
            "Expired",
            "Converted",
            "All"
        });

        public ICommand LoadCommand { get; }
        public ICommand OpenSelectedCommand { get; }
        public ICommand MarkSentCommand { get; }
        public ICommand MarkAcceptedCommand { get; }
        public ICommand MarkDeclinedCommand { get; }
        public ICommand ConvertSelectedCommand { get; }
        public ICommand DeleteSelectedCommand { get; }

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

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                if (string.Equals(_statusFilter, value, StringComparison.Ordinal))
                {
                    return;
                }

                _statusFilter = value;
                OnPropertyChanged(nameof(StatusFilter));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (string.Equals(_searchText, value, StringComparison.Ordinal))
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public QuoteLookupDto? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value))
                {
                    return;
                }

                _selectedItem = value;
                OnPropertyChanged(nameof(SelectedItem));
            }
        }

        public QuoteDetailsDto? SelectedQuote
        {
            get => _selectedQuote;
            private set
            {
                _selectedQuote = value;
                OnPropertyChanged(nameof(SelectedQuote));
                OnPropertyChanged(nameof(SelectedQuoteHasItems));
                OnPropertyChanged(nameof(CanMarkSent));
                OnPropertyChanged(nameof(CanMarkAccepted));
                OnPropertyChanged(nameof(CanMarkDeclined));
                OnPropertyChanged(nameof(CanConvert));
                OnPropertyChanged(nameof(CanDelete));
            }
        }

        public bool SelectedQuoteHasItems => SelectedQuote?.Items.Count > 0;
        public bool CanMarkSent => string.Equals(SelectedQuote?.Status, "Draft", StringComparison.OrdinalIgnoreCase);
        public bool CanMarkAccepted => string.Equals(SelectedQuote?.Status, "Sent", StringComparison.OrdinalIgnoreCase);
        public bool CanMarkDeclined => string.Equals(SelectedQuote?.Status, "Sent", StringComparison.OrdinalIgnoreCase);
        public bool CanConvert => SelectedQuote is not null
            && (string.Equals(SelectedQuote.Status, "Draft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(SelectedQuote.Status, "Accepted", StringComparison.OrdinalIgnoreCase));
        public bool CanDelete => SelectedQuote is not null;

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading quotes...";
            StatusBrush = Brushes.LightGray;

            try
            {
                var url = BuildLookupUrl();
                var items = await _crudApi.GetAllAsync<QuoteLookupDto>(url);
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }

                SelectedItem = null;
                SelectedQuote = null;
                OnPropertyChanged(nameof(SelectedItem));

                Status = $"{items.Count} quote(s) loaded.";
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

        private async Task OpenSelectedAsync()
        {
            if (SelectedItem is not { Id: > 0 })
            {
                SetFailure("Select a quote first.");
                return;
            }

            IsLoading = true;
            Status = "Loading quote details...";
            StatusBrush = Brushes.LightGray;

            try
            {
                SelectedQuote = await _crudApi.GetAsync<QuoteDetailsDto>($"api/quotes/{SelectedItem.Id}");
                Status = $"Quote {SelectedQuote.QuoteNumber} loaded.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                SetFailure(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateSelectedStatusAsync(string nextStatus)
        {
            if (SelectedQuote is not { Id: > 0 })
            {
                SetFailure("Open a quote first.");
                return;
            }

            IsLoading = true;
            Status = $"Updating quote to {nextStatus}...";
            StatusBrush = Brushes.LightGray;

            try
            {
                await _crudApi.PutAsync(
                    $"api/quotes/{SelectedQuote.Id}/status",
                    new UpdateQuoteStatusRequest { Status = nextStatus });

                await ReloadSelectedAsync($"{SelectedQuote.QuoteNumber} marked {nextStatus}.");
            }
            catch (Exception ex)
            {
                SetFailure(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ConvertSelectedAsync()
        {
            if (SelectedQuote is not { Id: > 0 })
            {
                SetFailure("Open a quote first.");
                return;
            }

            IsLoading = true;
            Status = "Converting quote to invoice...";
            StatusBrush = Brushes.LightGray;

            try
            {
                var response = await _crudApi.PostAsync<ConvertQuoteToInvoiceResponse>($"api/quotes/{SelectedQuote.Id}/convert", new { });
                await ReloadSelectedAsync($"Invoice {response.InvoiceNumber} created from {SelectedQuote.QuoteNumber}.");
            }
            catch (Exception ex)
            {
                SetFailure(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedQuote is not { Id: > 0 })
            {
                SetFailure("Open a quote first.");
                return;
            }

            var quoteNumber = SelectedQuote.QuoteNumber;

            IsLoading = true;
            Status = $"Deleting quote {quoteNumber}...";
            StatusBrush = Brushes.LightGray;

            try
            {
                await _crudApi.DeleteAsync($"api/quotes/{SelectedQuote.Id}");
                await LoadAsync();
                Status = $"Quote {quoteNumber} deleted.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                SetFailure(ex.Message);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ReloadSelectedAsync(string successMessage)
        {
            var selectedId = SelectedQuote?.Id;
            await LoadAsync();

            if (selectedId is > 0)
            {
                SelectedItem = Items.FirstOrDefault(item => item.Id == selectedId);
                OnPropertyChanged(nameof(SelectedItem));
                if (SelectedItem is not null)
                {
                    SelectedQuote = await _crudApi.GetAsync<QuoteDetailsDto>($"api/quotes/{selectedId}");
                }
            }

            Status = successMessage;
            StatusBrush = Brushes.LightGreen;
        }

        private string BuildLookupUrl()
        {
            var query = new Collection<string>();
            if (!string.Equals(StatusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                query.Add($"status={Uri.EscapeDataString(StatusFilter)}");
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
            }

            return query.Count == 0
                ? "api/quotes"
                : $"api/quotes?{string.Join("&", query)}";
        }

        private void HandleBackgroundException(Exception ex)
        {
            SetFailure(ex.Message);
            IsLoading = false;
        }

        private void SetFailure(string message)
        {
            Status = message;
            StatusBrush = Brushes.OrangeRed;
        }

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
