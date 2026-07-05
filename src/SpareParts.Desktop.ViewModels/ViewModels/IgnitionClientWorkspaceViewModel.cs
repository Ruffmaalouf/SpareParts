using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>
    /// Ignition client workspace view model — consumes the new customer-360 endpoints
    /// GET /api/clients/{id}/workspace, the paged child routes (/invoices, /payments, /quotes,
    /// /part-requests, /timeline) and the /api/clients/search typeahead (Report 04 §05–§06).
    /// Additive: no existing view or endpoint usage changes.
    /// </summary>
    public sealed class IgnitionClientWorkspaceViewModel : INotifyPropertyChanged
    {
        private const int PageSize = 25;
        private const double AgingBarMaxWidth = 160d;

        private const int InvoicesTab = 0;
        private const int PaymentsTab = 1;
        private const int QuotesTab = 2;
        private const int PartRequestsTab = 3;
        private const int TimelineTab = 4;

        private readonly IClientWorkspaceApiClient _api;

        private string _searchText = string.Empty;
        private bool _isSearching;
        private bool _isLoading;
        private bool _isTabLoading;
        private string _status = "Search for a client to open their workspace.";
        private Brush _statusBrush = Brushes.LightGray;
        private IgnitionClientWorkspaceDto? _client;
        private int _selectedTabIndex;

        private readonly bool[] _tabLoaded = new bool[5];
        private readonly int[] _tabPage = new int[5];

        public IgnitionClientWorkspaceViewModel(IClientWorkspaceApiClient api)
        {
            _api = api;
            SearchCommand = new RelayCommand(_ => SearchAsync().SafeFireAndForget(HandleBackgroundException));
            OpenClientCommand = new RelayCommand(o =>
            {
                if (o is IgnitionClientSearchResultDto picked)
                {
                    LoadWorkspaceAsync(picked.CustomerId).SafeFireAndForget(HandleBackgroundException);
                }
            });
            RefreshCommand = new RelayCommand(
                _ =>
                {
                    if (_client != null)
                    {
                        LoadWorkspaceAsync(_client.CustomerId).SafeFireAndForget(HandleBackgroundException);
                    }
                },
                _ => _client != null);
            LoadMoreCommand = new RelayCommand(
                _ => LoadSelectedTabAsync(append: true).SafeFireAndForget(HandleBackgroundException),
                _ => _client != null && !_isTabLoading);
        }

        public ObservableCollection<IgnitionClientSearchResultDto> SearchResults { get; } = new();
        public ObservableCollection<IgnitionAgingBucketModel> AgingBuckets { get; } = new();
        public ObservableCollection<IgnitionClientVehicleDto> Vehicles { get; } = new();
        public ObservableCollection<IgnitionClientInvoiceRowDto> Invoices { get; } = new();
        public ObservableCollection<IgnitionClientPaymentRowDto> Payments { get; } = new();
        public ObservableCollection<QuoteLookupDto> Quotes { get; } = new();
        public ObservableCollection<PartRequestDto> PartRequests { get; } = new();
        public ObservableCollection<IgnitionClientTimelineEventDto> TimelineEvents { get; } = new();

        public ICommand SearchCommand { get; }
        public ICommand OpenClientCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand LoadMoreCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public bool IsSearching
        {
            get => _isSearching;
            private set
            {
                if (_isSearching == value)
                {
                    return;
                }

                _isSearching = value;
                OnPropertyChanged(nameof(IsSearching));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex == value)
                {
                    return;
                }

                _selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
                EnsureSelectedTabLoadedAsync().SafeFireAndForget(HandleBackgroundException);
            }
        }

        public bool HasClient => _client != null;
        public bool HasNoClient => !HasClient;
        public bool HasSearchResults => SearchResults.Count > 0;
        public bool HasVehicles => Vehicles.Count > 0;

        public string ClientName => _client?.Name ?? string.Empty;

        public string ClientInitials
        {
            get
            {
                if (_client == null || string.IsNullOrWhiteSpace(_client.Name))
                {
                    return string.Empty;
                }

                var parts = _client.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return parts.Length == 1
                    ? parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant()
                    : $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}";
            }
        }

        public string ClientSubtitle
        {
            get
            {
                if (_client == null)
                {
                    return string.Empty;
                }

                var pieces = new[] { _client.CustomerType, _client.Phone, _client.Email }
                    .Where(piece => !string.IsNullOrWhiteSpace(piece));
                return string.Join(" · ", pieces);
            }
        }

        public string ClientStateText => _client == null ? string.Empty : (_client.IsActive ? "Active" : "Inactive");
        public string TotalBalanceText => _client == null ? string.Empty : Money(_client.Balance.TotalBalance);

        public string LastActivityText => _client?.LastActivityAt == null
            ? "No recent activity"
            : $"Last activity {_client.LastActivityAt.Value.ToString("dd MMM yyyy HH:mm", CultureInfo.CurrentCulture)}";

        public string LifetimeRevenueText => _client == null ? string.Empty : Money(_client.Kpis.LifetimeRevenue);
        public string LifetimeInvoiceCountText => _client == null ? string.Empty : _client.Kpis.LifetimeInvoiceCount.ToString("N0", CultureInfo.CurrentCulture);
        public string AverageDaysToPayText => _client == null ? string.Empty : $"{_client.Kpis.AverageDaysToPay} days";
        public string OpenQuotesText => _client == null ? string.Empty : _client.Kpis.OpenQuotesCount.ToString("N0", CultureInfo.CurrentCulture);
        public string ActivePartRequestsText => _client == null ? string.Empty : _client.Kpis.ActivePartRequestsCount.ToString("N0", CultureInfo.CurrentCulture);

        public string LoyaltyText
        {
            get
            {
                if (_client == null)
                {
                    return string.Empty;
                }

                return string.IsNullOrWhiteSpace(_client.Kpis.LoyaltyTier)
                    ? _client.Kpis.LoyaltyPoints.ToString("N0", CultureInfo.CurrentCulture)
                    : $"{_client.Kpis.LoyaltyTier} · {_client.Kpis.LoyaltyPoints:N0} pts";
            }
        }

        public async Task SearchAsync()
        {
            var query = SearchText?.Trim() ?? string.Empty;
            if (query.Length < 2)
            {
                Status = "Type at least 2 characters to search clients.";
                StatusBrush = Brushes.LightGray;
                return;
            }

            if (IsSearching)
            {
                return;
            }

            IsSearching = true;
            try
            {
                var results = await _api.SearchClientsAsync(query);
                SearchResults.Clear();
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }

                Status = results.Count == 0
                    ? $"No clients matched \"{query}\"."
                    : $"{results.Count:N0} client(s) matched \"{query}\".";
                StatusBrush = Brushes.LightGray;
                OnPropertyChanged(nameof(HasSearchResults));
            }
            catch (ApiClientException ex)
            {
                Status = $"Client search failed ({ex.Code}): {ex.Message}";
                StatusBrush = Accent("#EF4444");
            }
            finally
            {
                IsSearching = false;
            }
        }

        public async Task LoadWorkspaceAsync(int customerId)
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Opening client workspace...";
            StatusBrush = Brushes.LightGray;

            try
            {
                var workspace = await _api.GetWorkspaceAsync(customerId);
                ApplyWorkspace(workspace);
                Status = $"Workspace loaded for {workspace.Name}.";
                StatusBrush = Accent("#22C55E");
                await LoadSelectedTabAsync(append: false);
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Accent("#EF4444");
            }
            catch (Exception)
            {
                Status = "Unexpected error while loading the client workspace.";
                StatusBrush = Accent("#EF4444");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyWorkspace(IgnitionClientWorkspaceDto workspace)
        {
            _client = workspace;

            AgingBuckets.Clear();
            var balance = workspace.Balance;
            var maxBucket = new[]
            {
                balance.Current, balance.Days1To30, balance.Days31To60, balance.Days61To90, balance.Over90Days
            }.Max();
            AgingBuckets.Add(AgingBucket("Current", balance.Current, maxBucket, "#22C55E"));
            AgingBuckets.Add(AgingBucket("1-30 d", balance.Days1To30, maxBucket, "#FBBF24"));
            AgingBuckets.Add(AgingBucket("31-60 d", balance.Days31To60, maxBucket, "#FBBF24"));
            AgingBuckets.Add(AgingBucket("61-90 d", balance.Days61To90, maxBucket, "#EF4444"));
            AgingBuckets.Add(AgingBucket("90+ d", balance.Over90Days, maxBucket, "#EF4444"));

            Vehicles.Clear();
            foreach (var vehicle in workspace.Vehicles)
            {
                Vehicles.Add(vehicle);
            }

            Invoices.Clear();
            Payments.Clear();
            Quotes.Clear();
            PartRequests.Clear();
            TimelineEvents.Clear();
            Array.Clear(_tabLoaded, 0, _tabLoaded.Length);
            Array.Clear(_tabPage, 0, _tabPage.Length);

            OnPropertyChanged(nameof(HasClient));
            OnPropertyChanged(nameof(HasNoClient));
            OnPropertyChanged(nameof(HasVehicles));
            OnPropertyChanged(nameof(ClientName));
            OnPropertyChanged(nameof(ClientInitials));
            OnPropertyChanged(nameof(ClientSubtitle));
            OnPropertyChanged(nameof(ClientStateText));
            OnPropertyChanged(nameof(TotalBalanceText));
            OnPropertyChanged(nameof(LastActivityText));
            OnPropertyChanged(nameof(LifetimeRevenueText));
            OnPropertyChanged(nameof(LifetimeInvoiceCountText));
            OnPropertyChanged(nameof(AverageDaysToPayText));
            OnPropertyChanged(nameof(OpenQuotesText));
            OnPropertyChanged(nameof(ActivePartRequestsText));
            OnPropertyChanged(nameof(LoyaltyText));
        }

        private Task EnsureSelectedTabLoadedAsync()
        {
            if (_client == null || _tabLoaded[SelectedTabIndex])
            {
                return Task.CompletedTask;
            }

            return LoadSelectedTabAsync(append: false);
        }

        private async Task LoadSelectedTabAsync(bool append)
        {
            if (_client == null || _isTabLoading)
            {
                return;
            }

            _isTabLoading = true;
            var tab = SelectedTabIndex;
            var page = append ? _tabPage[tab] + 1 : 1;

            try
            {
                switch (tab)
                {
                    case InvoicesTab:
                        Fill(Invoices, await _api.GetInvoicesAsync(_client.CustomerId, page, PageSize), append);
                        break;
                    case PaymentsTab:
                        Fill(Payments, await _api.GetPaymentsAsync(_client.CustomerId, page, PageSize), append);
                        break;
                    case QuotesTab:
                        Fill(Quotes, await _api.GetQuotesAsync(_client.CustomerId, page, PageSize), append);
                        break;
                    case PartRequestsTab:
                        Fill(PartRequests, await _api.GetPartRequestsAsync(_client.CustomerId, page, PageSize), append);
                        break;
                    case TimelineTab:
                        Fill(TimelineEvents, await _api.GetTimelineAsync(_client.CustomerId, page, PageSize), append);
                        break;
                    default:
                        return;
                }

                _tabLoaded[tab] = true;
                _tabPage[tab] = page;
            }
            catch (ApiClientException ex)
            {
                Status = $"Failed to load workspace tab ({ex.Code}): {ex.Message}";
                StatusBrush = Accent("#EF4444");
            }
            finally
            {
                _isTabLoading = false;
            }
        }

        private static void Fill<T>(ObservableCollection<T> target, System.Collections.Generic.List<T> rows, bool append)
        {
            if (!append)
            {
                target.Clear();
            }

            foreach (var row in rows)
            {
                target.Add(row);
            }
        }

        private IgnitionAgingBucketModel AgingBucket(string label, decimal amount, decimal maxBucket, string accentHex)
            => new()
            {
                Label = label,
                AmountText = Money(amount),
                BarWidth = maxBucket <= 0m ? 0d : (double)(amount / maxBucket) * AgingBarMaxWidth,
                BarBrush = Accent(accentHex)
            };

        private string Money(decimal value)
            => $"{_client?.CurrencyCode ?? "USD"} {value:N2}";

        private static Brush Accent(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private void HandleBackgroundException(Exception ex)
        {
            Status = $"Client workspace task failed: {ex.Message}";
            StatusBrush = Accent("#EF4444");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
