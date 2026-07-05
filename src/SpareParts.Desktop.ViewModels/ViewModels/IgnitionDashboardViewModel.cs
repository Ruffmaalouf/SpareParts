using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Helpers;
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
    /// Ignition dashboard view model — consumes the new composed endpoint
    /// GET /api/dashboard/summary (Report 04 §02–§03). Additive: the legacy
    /// OwnerCockpitDashboardViewModel and /api/owner-cockpit keep working unchanged.
    /// </summary>
    public sealed class IgnitionDashboardViewModel : INotifyPropertyChanged
    {
        private const double TrendBarMaxHeight = 96d;

        private readonly IIgnitionDashboardApiClient _api;
        private DateTime _businessDate = DateTime.Today;
        private DateTime? _lastRefreshedAt;
        private string _currencyCode = "USD";
        private string _status = "Ignition dashboard ready.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;

        public IgnitionDashboardViewModel(IIgnitionDashboardApiClient api)
        {
            _api = api;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public ObservableCollection<IgnitionKpiTileModel> KpiTiles { get; } = new();
        public ObservableCollection<IgnitionActionQueueItemModel> ActionQueue { get; } = new();
        public ObservableCollection<IgnitionTrendBarModel> TrendBars { get; } = new();
        public ObservableCollection<IgnitionLowStockItemDto> LowStockItems { get; } = new();
        public ObservableCollection<IgnitionDeadStockSegmentDto> DeadStockSegments { get; } = new();
        public ObservableCollection<IgnitionUnpaidTransactionItemDto> UnpaidItems { get; } = new();

        public ICommand LoadCommand { get; }

        public DateTime BusinessDate
        {
            get => _businessDate;
            private set
            {
                if (_businessDate == value)
                {
                    return;
                }

                _businessDate = value;
                OnPropertyChanged(nameof(BusinessDate));
                OnPropertyChanged(nameof(BusinessDateText));
            }
        }

        public string BusinessDateText => BusinessDate.ToString("dddd, dd MMM yyyy", CultureInfo.CurrentCulture);

        public DateTime? LastRefreshedAt
        {
            get => _lastRefreshedAt;
            private set
            {
                if (_lastRefreshedAt == value)
                {
                    return;
                }

                _lastRefreshedAt = value;
                OnPropertyChanged(nameof(LastRefreshedAt));
                OnPropertyChanged(nameof(LastRefreshedText));
            }
        }

        public string LastRefreshedText => LastRefreshedAt == null
            ? "Not refreshed yet"
            : $"Updated {LastRefreshedAt.Value:t}";

        public string CurrencyCode
        {
            get => _currencyCode;
            private set
            {
                if (_currencyCode == value)
                {
                    return;
                }

                _currencyCode = value;
                OnPropertyChanged(nameof(CurrencyCode));
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

        public bool HasActionQueue => ActionQueue.Count > 0;
        public bool HasNoActionQueue => !HasActionQueue;
        public bool HasTrendBars => TrendBars.Count > 0;
        public bool HasNoTrendBars => !HasTrendBars;
        public bool HasLowStockItems => LowStockItems.Count > 0;
        public bool HasNoLowStockItems => !HasLowStockItems;
        public bool HasDeadStockSegments => DeadStockSegments.Count > 0;
        public bool HasNoDeadStockSegments => !HasDeadStockSegments;
        public bool HasUnpaidItems => UnpaidItems.Count > 0;
        public bool HasNoUnpaidItems => !HasUnpaidItems;

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Refreshing Ignition dashboard...";
            StatusBrush = Brushes.LightGray;

            try
            {
                var summary = await _api.GetSummaryAsync(DateTime.Today);
                ApplySummary(summary);
                Status = "Ignition dashboard updated.";
                StatusBrush = Accent("#22C55E");
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Accent("#EF4444");
            }
            catch (Exception)
            {
                Status = "Unexpected error while loading the Ignition dashboard.";
                StatusBrush = Accent("#EF4444");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplySummary(IgnitionDashboardSummaryDto summary)
        {
            CurrencyCode = string.IsNullOrWhiteSpace(summary.CurrencyCode)
                ? "USD"
                : summary.CurrencyCode.Trim().ToUpperInvariant();
            BusinessDate = summary.BusinessDate.Date;
            LastRefreshedAt = summary.GeneratedAt == default ? DateTime.Now : summary.GeneratedAt;

            KpiTiles.Clear();
            KpiTiles.Add(KpiTile("Sales today", Money(summary.Kpis.SalesToday.Value), summary.Kpis.SalesToday));
            KpiTiles.Add(KpiTile("Profit today", Money(summary.Kpis.ProfitToday.Value), summary.Kpis.ProfitToday));
            KpiTiles.Add(KpiTile("Cash balance", Money(summary.Kpis.CashBalance.Value), summary.Kpis.CashBalance));
            KpiTiles.Add(KpiTile("Receivables", Money(summary.Kpis.Receivables.Value), summary.Kpis.Receivables));
            KpiTiles.Add(KpiTile("Stock value", Money(summary.Kpis.StockValue.Value), summary.Kpis.StockValue));
            KpiTiles.Add(KpiTile("Low stock", summary.Kpis.LowStockCount.Value.ToString("N0", CultureInfo.CurrentCulture), summary.Kpis.LowStockCount));

            ActionQueue.Clear();
            foreach (var item in summary.ActionQueue)
            {
                ActionQueue.Add(new IgnitionActionQueueItemModel
                {
                    Title = item.Title,
                    Detail = item.Detail,
                    AmountText = item.Amount.HasValue ? Money(item.Amount.Value) : null,
                    Severity = item.Severity,
                    SeverityBrush = SeverityBrush(item.Severity),
                    DeepLink = item.DeepLink
                });
            }

            TrendBars.Clear();
            var maxAmount = summary.SalesTrend.Series.Count == 0
                ? 0m
                : summary.SalesTrend.Series.Max(point => point.Amount);
            foreach (var point in summary.SalesTrend.Series)
            {
                var isPeak = summary.SalesTrend.PeakDate.HasValue
                    && point.Date.Date == summary.SalesTrend.PeakDate.Value.Date;
                TrendBars.Add(new IgnitionTrendBarModel
                {
                    DateLabel = point.Date.ToString("ddd", CultureInfo.CurrentCulture),
                    AmountText = Money(point.Amount),
                    BarHeight = maxAmount <= 0m ? 0d : (double)(point.Amount / maxAmount) * TrendBarMaxHeight,
                    IsPeak = isPeak,
                    BarBrush = isPeak ? Accent("#F97316") : Accent("#3A3A41")
                });
            }

            Replace(LowStockItems, summary.LowStockPanel.TopItems);
            Replace(DeadStockSegments, summary.DeadStockPanel.TopSegments);
            Replace(UnpaidItems, summary.UnpaidTransactionsPanel.TopItems);

            OnPropertyChanged(nameof(HasActionQueue));
            OnPropertyChanged(nameof(HasNoActionQueue));
            OnPropertyChanged(nameof(HasTrendBars));
            OnPropertyChanged(nameof(HasNoTrendBars));
            OnPropertyChanged(nameof(HasLowStockItems));
            OnPropertyChanged(nameof(HasNoLowStockItems));
            OnPropertyChanged(nameof(HasDeadStockSegments));
            OnPropertyChanged(nameof(HasNoDeadStockSegments));
            OnPropertyChanged(nameof(HasUnpaidItems));
            OnPropertyChanged(nameof(HasNoUnpaidItems));
        }

        private IgnitionKpiTileModel KpiTile(string title, string value, IgnitionKpiValueDto kpi)
            => new()
            {
                Title = title,
                Value = value,
                Delta = PercentDelta(kpi.DeltaPercent),
                DeltaBrush = kpi.DeltaPercent switch
                {
                    null => Accent("#6B6B74"),
                    < 0m => Accent("#EF4444"),
                    _ => Accent("#22C55E")
                }
            };

        private static void Replace<T>(ObservableCollection<T> target, System.Collections.Generic.IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string? PercentDelta(decimal? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value < 0
                ? $"-{Math.Abs(value.Value):N1}%"
                : $"+{value.Value:N1}%";
        }

        private static Brush SeverityBrush(string severity)
            => severity?.Trim().ToLowerInvariant() switch
            {
                "danger" => Accent("#EF4444"),
                "warning" => Accent("#FBBF24"),
                _ => Accent("#38BDF8")
            };

        private string Money(decimal value)
            => $"{CurrencyCode} {value:N2}";

        private static Brush Accent(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        private void HandleBackgroundException(Exception ex)
        {
            Status = $"Ignition dashboard task failed: {ex.Message}";
            StatusBrush = Accent("#EF4444");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
