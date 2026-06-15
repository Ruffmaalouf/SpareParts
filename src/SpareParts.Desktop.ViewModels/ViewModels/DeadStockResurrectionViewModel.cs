using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class DeadStockResurrectionViewModel : INotifyPropertyChanged
    {
        private readonly IPartsApiClient _partsApi;
        private DeadStockItemRow? _selectedItem;
        private string _filterText = string.Empty;
        private string _status = "Load dead-stock candidates to plan recovery actions.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;
        private int _minDormantDays = 90;
        private int _take = 35;
        private DeadStockReportDto? _lastReport;

        public DeadStockResurrectionViewModel(IPartsApiClient partsApi)
        {
            _partsApi = partsApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshCommand = LoadCommand;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<DeadStockItemRow> Items { get; } = new();
        public ObservableCollection<DeadStockItemRow> FilteredItems { get; } = new();
        public ObservableCollection<DeadStockMetricTile> MetricTiles { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }

        public int MinDormantDays
        {
            get => _minDormantDays;
            set
            {
                var normalized = Math.Clamp(value, 30, 720);
                if (_minDormantDays == normalized)
                {
                    return;
                }

                _minDormantDays = normalized;
                OnPropertyChanged(nameof(MinDormantDays));
                OnPropertyChanged(nameof(ThresholdSummary));
            }
        }

        public int Take
        {
            get => _take;
            set
            {
                var normalized = Math.Clamp(value, 1, 100);
                if (_take == normalized)
                {
                    return;
                }

                _take = normalized;
                OnPropertyChanged(nameof(Take));
                OnPropertyChanged(nameof(ThresholdSummary));
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText == value)
                {
                    return;
                }

                _filterText = value ?? string.Empty;
                OnPropertyChanged(nameof(FilterText));
                RefreshFilteredItems();
            }
        }

        public DeadStockItemRow? SelectedItem
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
                OnPropertyChanged(nameof(SelectedPartTitle));
                OnPropertyChanged(nameof(SelectedPartSubtitle));
                OnPropertyChanged(nameof(SelectedDormancyDetail));
            }
        }

        public string SelectedPartTitle => SelectedItem?.PartName ?? "Select dead stock";

        public string SelectedPartSubtitle => SelectedItem == null
            ? "Pick an aged inventory row to review the recovery plan."
            : $"{SelectedItem.DisplayCode} - OEM {SelectedItem.OemDisplay}";

        public string SelectedDormancyDetail => SelectedItem == null
            ? "No part selected."
            : $"{SelectedItem.DormancyLabel}. Last receipt {SelectedItem.LastReceivedLabel}. Last sale {SelectedItem.LastSoldLabel}.";

        public string ThresholdSummary => $"Dormant {MinDormantDays:N0}+ days, showing top {Take:N0}.";

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

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Finding dead-stock candidates...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var report = await _partsApi.GetDeadStockAsync(MinDormantDays, Take);
                var rows = report.Items
                    .Select(DeadStockItemRow.From)
                    .OrderByDescending(item => item.DormantDays)
                    .ThenByDescending(item => item.StockValue)
                    .ToList();

                _lastReport = report;
                Replace(Items, rows);
                RefreshFilteredItems(selectFirstWhenMissing: true);
                RebuildMetrics(report, rows);

                Status = rows.Count == 0
                    ? $"No dead stock found beyond {report.MinDormantDays:N0} days."
                    : $"Found {rows.Count:N0} dead-stock candidate(s).";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                Status = "Could not load dead-stock report.";
                StatusBrush = Brushes.IndianRed;
                AppNotificationCenter.Instance.Publish($"Dead stock report failed: {ex.Message}", false);
                Items.Clear();
                FilteredItems.Clear();
                MetricTiles.Clear();
                SelectedItem = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshFilteredItems(bool selectFirstWhenMissing = false)
        {
            var query = Normalize(FilterText);
            var rows = string.IsNullOrWhiteSpace(query)
                ? Items.ToList()
                : Items.Where(item => item.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            Replace(FilteredItems, rows);

            if (FilteredItems.Count == 0)
            {
                SelectedItem = null;
                return;
            }

            if (SelectedItem == null && selectFirstWhenMissing)
            {
                SelectedItem = FilteredItems[0];
                return;
            }

            if (SelectedItem != null && !FilteredItems.Contains(SelectedItem))
            {
                SelectedItem = FilteredItems[0];
            }
        }

        private void RebuildMetrics(DeadStockReportDto report, IReadOnlyCollection<DeadStockItemRow> rows)
        {
            var worstDormant = rows.Count == 0 ? 0 : rows.Max(item => item.DormantDays);
            var totalUnits = rows.Sum(item => item.OnHand);
            var stockValue = rows.Sum(item => item.StockValue);
            var currency = rows.Select(item => item.Currency).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
                ? rows.FirstOrDefault()?.Currency ?? report.DominantCurrency
                : report.DominantCurrency;

            Replace(MetricTiles, new[]
            {
                new DeadStockMetricTile("Candidates", rows.Count.ToString("N0", CultureInfo.CurrentCulture), "Aged on-hand parts", Accent("#FFFF7043")),
                new DeadStockMetricTile("Units", totalUnits.ToString("N0", CultureInfo.CurrentCulture), "Stock still on shelf", Accent("#FFFFB74D")),
                new DeadStockMetricTile("Value", currency == "MIXED" ? "Mixed" : DeadStockItemRow.FormatMoney(stockValue, currency), "Estimated inventory value", Accent("#FF81C784")),
                new DeadStockMetricTile("Oldest", $"{worstDormant:N0}d", "Longest dormant row", Accent("#FF64B5F6"))
            });
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ').ToArray();
            return string.Join(" ", new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private static SolidColorBrush Accent(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }

        private static void HandleBackgroundException(Exception ex)
            => AppNotificationCenter.Instance.Publish($"Dead stock report error: {ex.Message}", false);

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
