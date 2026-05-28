using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.OwnerCockpit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class OwnerCockpitDashboardViewModel : INotifyPropertyChanged
    {
        private readonly IOwnerCockpitApiClient _api;
        private DateTime _businessDate = DateTime.Today;
        private DateTime? _lastRefreshedAt;
        private string _currencyCode = "USD";
        private string _status = "Owner cockpit ready.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;

        public OwnerCockpitDashboardViewModel(IOwnerCockpitApiClient api)
        {
            _api = api;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
        }

        public ObservableCollection<OwnerCockpitMetricCard> Metrics { get; } = new();
        public ObservableCollection<OwnerCockpitProfitRowDto> ProfitPerCar { get; } = new();
        public ObservableCollection<OwnerCockpitProfitRowDto> ProfitPerPart { get; } = new();
        public ObservableCollection<OwnerCockpitProfitHeatmapRowDto> ProfitHeatmap { get; } = new();
        public ObservableCollection<OwnerCockpitUnpaidTransactionDto> UnpaidTransactions { get; } = new();
        public ObservableCollection<OwnerCockpitAlertDto> AccountingAlerts { get; } = new();

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

        public bool HasProfitPerCar => ProfitPerCar.Count > 0;
        public bool HasProfitPerPart => ProfitPerPart.Count > 0;
        public bool HasProfitHeatmap => ProfitHeatmap.Count > 0;
        public bool HasUnpaidTransactions => UnpaidTransactions.Count > 0;
        public bool HasAccountingAlerts => AccountingAlerts.Count > 0;
        public bool HasNoAccountingAlerts => !HasAccountingAlerts;

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Refreshing owner cockpit...";
            StatusBrush = Brushes.LightGray;

            try
            {
                var dashboard = await _api.GetDashboardAsync(DateTime.Today);
                ApplyDashboard(dashboard);
                Status = "Owner cockpit updated.";
                StatusBrush = Accent("#FF81C784");
            }
            catch (ApiClientException ex)
            {
                Status = $"API error ({ex.Code}): {ex.Message}";
                StatusBrush = Accent("#FFE57373");
            }
            catch (Exception)
            {
                Status = "Unexpected error while loading owner cockpit.";
                StatusBrush = Accent("#FFE57373");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyDashboard(OwnerCockpitDashboardDto dashboard)
        {
            CurrencyCode = string.IsNullOrWhiteSpace(dashboard.CurrencyCode) ? "USD" : dashboard.CurrencyCode.Trim().ToUpperInvariant();
            BusinessDate = dashboard.BusinessDate.Date;
            LastRefreshedAt = dashboard.GeneratedAt == default ? DateTime.Now : dashboard.GeneratedAt;

            Metrics.Clear();
            Metrics.Add(Card("Today's sales", Money(dashboard.TodaySalesAmount), $"{dashboard.TodaySalesCount:N0} invoices | paid {Money(dashboard.TodaySalesPaidAmount)}", "#FF42A5F5"));
            Metrics.Add(Card("Net P&L today", Money(dashboard.DailyProfitLoss.NetProfitLoss), $"Gross profit {Money(dashboard.DailyProfitLoss.GrossProfit)} | expenses {Money(dashboard.DailyProfitLoss.TotalOperatingExpenses)}", dashboard.DailyProfitLoss.NetProfitLoss < 0m ? "#FFE57373" : "#FF81C784"));
            Metrics.Add(Card("Rent paid", Money(dashboard.DailyProfitLoss.RentExpense), "Daily rent expense journals", "#FFFFB74D"));
            Metrics.Add(Card("Labor paid", Money(dashboard.DailyProfitLoss.LaborExpense), "Daily payroll and staff journals", "#FFBA68C8"));
            Metrics.Add(Card("Today's purchases", Money(dashboard.TodayPurchasesAmount), $"{dashboard.TodayPurchasesCount:N0} bills | paid {Money(dashboard.TodayPurchasesPaidAmount)}", "#FFFFB74D"));
            Metrics.Add(Card("Cash balance", Money(dashboard.CashBalance), "Cash ledger balance", "#FF66BB6A"));
            Metrics.Add(Card("Supplier debt", Money(dashboard.SupplierDebt), "Open supplier balances", "#FFFF8A65"));
            Metrics.Add(Card("Customer debt", Money(dashboard.CustomerDebt), "Open customer balances", "#FFBA68C8"));
            Metrics.Add(Card("Stock value", Money(dashboard.StockValue), "Current inventory valuation", "#FF4DB6AC"));
            Metrics.Add(Card("Profit per car", Money(dashboard.TotalCarProfit), "Top linked used cars", "#FFFFD54F"));
            Metrics.Add(Card("Profit per part", Money(dashboard.TotalPartProfit), "Top parts today", "#FF90CAF9"));
            Metrics.Add(Card("Unpaid open", Money(dashboard.UnpaidTransactionAmount), $"{dashboard.UnpaidTransactionCount:N0} transactions", "#FFE57373"));
            Metrics.Add(Card("Smart alerts", dashboard.AccountingAlertCount == 0 ? "Clear" : dashboard.AccountingAlertCount.ToString("N0"), "Stock, balances, setup checks", "#FFAED581"));

            Replace(ProfitPerCar, dashboard.ProfitPerCar);
            Replace(ProfitPerPart, dashboard.ProfitPerPart);
            Replace(ProfitHeatmap, dashboard.ProfitHeatmap);
            Replace(UnpaidTransactions, dashboard.UnpaidTransactions);
            Replace(AccountingAlerts, dashboard.AccountingAlerts);

            OnPropertyChanged(nameof(HasProfitPerCar));
            OnPropertyChanged(nameof(HasProfitPerPart));
            OnPropertyChanged(nameof(HasProfitHeatmap));
            OnPropertyChanged(nameof(HasUnpaidTransactions));
            OnPropertyChanged(nameof(HasAccountingAlerts));
            OnPropertyChanged(nameof(HasNoAccountingAlerts));
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private OwnerCockpitMetricCard Card(string title, string value, string caption, string accentHex)
            => new()
            {
                Title = title,
                Value = value,
                Caption = caption,
                AccentBrush = Accent(accentHex)
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
            Status = $"Owner cockpit task failed: {ex.Message}";
            StatusBrush = Accent("#FFE57373");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
