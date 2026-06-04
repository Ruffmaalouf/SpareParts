using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Pricing;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class PartManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private IFilePickerService? _filePickerService;
        private IUserNotificationService? _notificationService;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private Func<string>? _getDefaultCurrencyCode;
        private string _newPartCode = string.Empty;
        private string _newPartBarcode = string.Empty;
        private string _newPartName = string.Empty;
        private string _newPartOEM = string.Empty;
        private decimal _newPartCostPrice;
        private decimal _newPartSalePrice;
        private string _newPartAveragePrice = string.Empty;
        private string _newPartEstimatedMarketPrice = string.Empty;
        private string _newPartCurrency = "USD";
        private int _newPartMinStock;
        private int _newPartCategoryId = 1;
        private int? _newPartBrandId;
        private string _newPartNotes = string.Empty;
        private string _partFilterText = string.Empty;
        private string _partAvailabilityFilter = "All";
        private IReadOnlyList<PartRequestDto> _partDemandRequests = Array.Empty<PartRequestDto>();
        private PartDto? _selectedPart;
        private SmartPricingCoachResult _pricingCoach = SmartPricingCoach.Evaluate(null);
        private bool _isGeneratingPartNotes;
        private bool _isImportingParts;

        public ObservableCollection<PartDto> Parts { get; } = new();
        public ObservableCollection<PartDto> FilteredParts { get; } = new();
        public ObservableCollection<CategoryDto> Categories { get; } = new();
        public ObservableCollection<BrandDto> BrandOptions { get; } = new();
        public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = new();
        public ObservableCollection<string> PartAvailabilityFilters { get; } = new(new[]
        {
            "All",
            "Available",
            "Reserved",
            "Sold",
            "Low stock",
            "Donor parts",
            "Demand matches",
            "Action needed",
            "Margin watch"
        });
        public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportFromExcelCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand GeneratePartNotesCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DuplicatePartDraftCommand { get; private set; } = new RelayCommand(_ => { });

        public bool IsGeneratingPartNotes
        {
            get => _isGeneratingPartNotes;
            private set
            {
                if (!SetProperty(ref _isGeneratingPartNotes, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(PartAiButtonText));
            }
        }

        public bool IsImportingParts
        {
            get => _isImportingParts;
            private set
            {
                if (!SetProperty(ref _isImportingParts, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(PartImportButtonText));
            }
        }

        public string PartAiButtonText => IsGeneratingPartNotes ? "AI is drafting notes..." : "✨ Draft Notes with AI";
        public string PartImportButtonText => IsImportingParts ? "Importing workbook..." : "⬆ Import Parts from Excel";
        public string SmartPricingCoachMessage => _pricingCoach.Message;
        public string SmartPricingCoachBadge => _pricingCoach.Badge;
        public string SmartPricingCoachTone => _pricingCoach.Tone;

        public void Configure(
            ManagementCoordinator coordinator,
            IFilePickerService filePickerService,
            IUserNotificationService notificationService,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            Func<string> getDefaultCurrencyCode,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _filePickerService = filePickerService;
            _notificationService = notificationService;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            _getDefaultCurrencyCode = getDefaultCurrencyCode;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ = ImportFromExcelAsync());
            GeneratePartNotesCommand = new RelayCommand(_ => _ = GeneratePartNotesAsync());
            DuplicatePartDraftCommand = new RelayCommand(_ => DuplicateSelectedPartDraft());
        }

        public void LoadReferenceData(
            IEnumerable<CategoryDto> categories,
            IEnumerable<BrandDto> brands,
            IEnumerable<CurrencyRateDto> currencyRates)
        {
            Replace(Categories, categories);
            Replace(BrandOptions, brands);
            Replace(CurrencyRates, currencyRates);
        }

        public void LoadParts(IEnumerable<PartDto> parts, IEnumerable<PartRequestDto>? partRequests = null)
        {
            Replace(Parts, parts);
            _partDemandRequests = (partRequests ?? Array.Empty<PartRequestDto>())
                .Where(request => PartRequestStatus.IsActive(request.Status))
                .ToList();
            RefreshFilteredParts(selectFirstWhenMissing: true);
            RaisePartInventorySummaries();
        }

        public string NewPartCode
        {
            get => _newPartCode;
            set => SetProperty(ref _newPartCode, value);
        }

        public string NewPartBarcode
        {
            get => _newPartBarcode;
            set => SetProperty(ref _newPartBarcode, value);
        }

        public string NewPartName
        {
            get => _newPartName;
            set => SetProperty(ref _newPartName, value);
        }

        public string NewPartOEM
        {
            get => _newPartOEM;
            set => SetProperty(ref _newPartOEM, value);
        }

        public decimal NewPartCostPrice
        {
            get => _newPartCostPrice;
            set
            {
                if (SetProperty(ref _newPartCostPrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public decimal NewPartSalePrice
        {
            get => _newPartSalePrice;
            set
            {
                if (SetProperty(ref _newPartSalePrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartAveragePrice
        {
            get => _newPartAveragePrice;
            set
            {
                if (SetProperty(ref _newPartAveragePrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartEstimatedMarketPrice
        {
            get => _newPartEstimatedMarketPrice;
            set
            {
                if (SetProperty(ref _newPartEstimatedMarketPrice, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public string NewPartCurrency
        {
            get => _newPartCurrency;
            set
            {
                if (SetProperty(ref _newPartCurrency, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public int NewPartMinStock
        {
            get => _newPartMinStock;
            set
            {
                if (SetProperty(ref _newPartMinStock, value))
                {
                    UpdatePricingCoach();
                }
            }
        }

        public int NewPartCategoryId
        {
            get => _newPartCategoryId;
            set => SetProperty(ref _newPartCategoryId, value);
        }

        public int? NewPartBrandId
        {
            get => _newPartBrandId;
            set => SetProperty(ref _newPartBrandId, value);
        }

        public string NewPartNotes
        {
            get => _newPartNotes;
            set => SetProperty(ref _newPartNotes, value);
        }

        public string PartFilterText
        {
            get => _partFilterText;
            set
            {
                if (!SetProperty(ref _partFilterText, value ?? string.Empty))
                {
                    return;
                }

                RefreshFilteredParts();
            }
        }

        public string PartAvailabilityFilter
        {
            get => _partAvailabilityFilter;
            set
            {
                var next = string.IsNullOrWhiteSpace(value) ? "All" : value;
                if (!SetProperty(ref _partAvailabilityFilter, next))
                {
                    return;
                }

                RefreshFilteredParts();
            }
        }

        public int TotalPartCount => Parts.Count;
        public int FilteredPartCount => FilteredParts.Count;
        public int AvailablePartCount => Parts.Count(part => part.AvailableQuantity > 0);
        public int ReservedPartCount => Parts.Count(part => part.ReservedQuantity > 0);
        public int SoldPartCount => Parts.Count(part => part.AvailableQuantity <= 0 && part.ReservedQuantity <= 0);
        public int LowStockPartCount => Parts.Count(part => part.AvailableQuantity <= part.MinStock);
        public int DonorPartCount => Parts.Count(part => part.UsedCarId.HasValue);
        public int DemandMatchPartCount => Parts.Count(part => GetDemandMatchCount(part) > 0);
        public int ActionNeededPartCount => Parts.Count(part => GetPartActionScore(part) > 0);
        public int MarginWatchPartCount => Parts.Count(IsMarginWatch);

        public string InventoryBoardSummary =>
            $"{FilteredPartCount:N0} shown from {TotalPartCount:N0}. {AvailablePartCount:N0} available, {ReservedPartCount:N0} reserved, {LowStockPartCount:N0} low-stock, {DemandMatchPartCount:N0} with demand.";

        public string TodayFocusSummary =>
            $"{ActionNeededPartCount:N0} action parts: {DemandMatchPartCount:N0} demand matches, {LowStockPartCount:N0} low-stock, {MarginWatchPartCount:N0} margin watch.";

        public string SelectedPartStockSummary => SelectedPart == null
            ? "Select a part to review stock, reservation, and donor status."
            : $"{SelectedPart.AvailableQuantity:N0} available, {SelectedPart.ReservedQuantity:N0} reserved, {SelectedPart.StockQuantity:N0} on hand.";

        public string SelectedPartPriceSummary => SelectedPart == null
            ? "No selected price."
            : $"{SelectedPart.Currency} sale {SelectedPart.SalePrice:N2}, cost {SelectedPart.CostPrice:N2}, min stock {SelectedPart.MinStock:N0}.";

        public string SelectedPartDonorSummary => SelectedPart?.UsedCarId is int usedCarId
            ? $"Linked to donor car #{usedCarId}."
            : "Standalone inventory item.";

        public string SelectedPartDemandSummary => SelectedPart == null
            ? "No selected demand signal."
            : GetDemandMatchCount(SelectedPart) == 0
                ? "No active customer demand match."
                : $"{GetDemandMatchCount(SelectedPart):N0} active customer demand match{(GetDemandMatchCount(SelectedPart) == 1 ? string.Empty : "es")}.";

        public string SelectedPartActionSummary => SelectedPart == null
            ? "No selected action."
            : GetPartActionLabel(SelectedPart);

        public PartDto? SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (!SetProperty(ref _selectedPart, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
                else
                {
                    UpdatePricingCoach();
                }

                OnPropertyChanged(nameof(SelectedPartStockSummary));
                OnPropertyChanged(nameof(SelectedPartPriceSummary));
                OnPropertyChanged(nameof(SelectedPartDonorSummary));
                OnPropertyChanged(nameof(SelectedPartDemandSummary));
                OnPropertyChanged(nameof(SelectedPartActionSummary));
            }
        }

        public void PopulateForm(PartDto p)
        {
            NewPartCode = p.InternalCode;
            NewPartBarcode = p.Barcode ?? string.Empty;
            NewPartName = p.Name;
            NewPartOEM = p.OEMNumber ?? string.Empty;
            NewPartCategoryId = p.CategoryId;
            NewPartBrandId = p.BrandId;
            NewPartCostPrice = p.CostPrice;
            NewPartSalePrice = p.SalePrice;
            NewPartAveragePrice = p.AveragePrice?.ToString("0.##") ?? string.Empty;
            NewPartEstimatedMarketPrice = p.EstimatedMarketPrice?.ToString("0.##") ?? string.Empty;
            NewPartCurrency = p.Currency;
            NewPartMinStock = p.MinStock;
            NewPartNotes = p.Notes ?? string.Empty;
            UpdatePricingCoach();
        }

        public void ClearForm(string defaultCurrencyCode = "USD")
        {
            NewPartCode = NewPartBarcode = NewPartName = NewPartOEM = NewPartAveragePrice = NewPartEstimatedMarketPrice = NewPartNotes = string.Empty;
            NewPartCostPrice = NewPartSalePrice = 0;
            NewPartCurrency = defaultCurrencyCode;
            NewPartMinStock = 0;
            NewPartCategoryId = 1;
            NewPartBrandId = null;
            SelectedPart = null;
            UpdatePricingCoach();
        }

        public void StartNew() => ClearForm(_getDefaultCurrencyCode?.Invoke() ?? "USD");

        private async Task SaveAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SavePartAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeletePartAsync(SelectedPart);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }

        private async Task GeneratePartNotesAsync()
        {
            if (_coordinator == null || _setStatus == null || IsGeneratingPartNotes)
            {
                return;
            }

            IsGeneratingPartNotes = true;
            _setStatus("Drafting part notes with AI…", true);

            try
            {
                var categoryLookup = Categories.ToDictionary(item => item.Id, item => item.Name);
                var brandLookup = BrandOptions.ToDictionary(item => item.Id, item => item.Name);
                var result = await _coordinator.GeneratePartNotesAsync(this, categoryLookup, brandLookup);
                _setStatus(result.Message, result.Success);
            }
            finally
            {
                IsGeneratingPartNotes = false;
            }
        }

        private async Task ImportFromExcelAsync()
        {
            if (_coordinator == null || _filePickerService == null || _notificationService == null || _refreshAsync == null || _setStatus == null || IsImportingParts)
            {
                return;
            }

            var filePath = _filePickerService
                .PickFiles(new FilePickerRequest
                {
                    Filter = "Excel workbook|*.xlsx",
                    AllowMultiple = false,
                    Title = "Import parts from Excel"
                })
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            IsImportingParts = true;
            _setStatus("Importing parts from Excel…", true);

            try
            {
                var result = await _coordinator.ImportPartsFromExcelAsync(filePath, Categories.ToList(), BrandOptions.ToList());
                _setStatus(result.SummaryMessage, result.HasImportedRows);

                if (result.HasImportedRows)
                {
                    await _refreshAsync();
                    StartNew();
                }

                _notificationService.Show(
                    result.ToDialogMessage(),
                    "Parts Import",
                    result.HasErrors
                        ? (result.HasImportedRows ? NotificationKind.Warning : NotificationKind.Error)
                        : NotificationKind.Success);
            }
            finally
            {
                IsImportingParts = false;
            }
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void DuplicateSelectedPartDraft()
        {
            if (SelectedPart == null)
            {
                _setStatus?.Invoke("Select a part before creating a duplicate draft.", false);
                return;
            }

            var source = SelectedPart;
            PopulateForm(source);
            NewPartCode = string.IsNullOrWhiteSpace(source.InternalCode)
                ? string.Empty
                : $"{source.InternalCode}-COPY";
            NewPartBarcode = string.Empty;
            SelectedPart = null;
            _setStatus?.Invoke("Duplicate draft prepared. Review the code, barcode, donor link, and pricing before saving.", true);
        }

        private void RefreshFilteredParts(bool selectFirstWhenMissing = false)
        {
            var query = Normalize(PartFilterText);
            var rows = Parts
                .Where(part => MatchesPartFilter(part, query) && MatchesAvailabilityFilter(part))
                .OrderByDescending(GetPartActionScore)
                .ThenByDescending(part => part.AvailableQuantity > 0)
                .ThenBy(part => part.Name)
                .ThenBy(part => part.Id)
                .ToList();

            Replace(FilteredParts, rows);

            if (FilteredParts.Count == 0)
            {
                SelectedPart = null;
            }
            else if (SelectedPart == null && selectFirstWhenMissing)
            {
                SelectedPart = FilteredParts[0];
            }
            else if (SelectedPart != null && !FilteredParts.Contains(SelectedPart))
            {
                SelectedPart = FilteredParts[0];
            }

            OnPropertyChanged(nameof(FilteredPartCount));
            OnPropertyChanged(nameof(InventoryBoardSummary));
            OnPropertyChanged(nameof(TodayFocusSummary));
        }

        private bool MatchesAvailabilityFilter(PartDto part)
            => PartAvailabilityFilter switch
            {
                "Available" => part.AvailableQuantity > 0,
                "Reserved" => part.ReservedQuantity > 0,
                "Sold" => part.AvailableQuantity <= 0 && part.ReservedQuantity <= 0,
                "Low stock" => part.AvailableQuantity <= part.MinStock,
                "Donor parts" => part.UsedCarId.HasValue,
                "Demand matches" => GetDemandMatchCount(part) > 0,
                "Action needed" => GetPartActionScore(part) > 0,
                "Margin watch" => IsMarginWatch(part),
                _ => true
            };

        private int GetDemandMatchCount(PartDto part)
            => _partDemandRequests.Count(request => RequestMatchesPart(request, part));

        private bool RequestMatchesPart(PartRequestDto request, PartDto part)
        {
            if (request.PartId == part.Id)
            {
                return true;
            }

            var requestedOem = Normalize(request.RequestedOemNumber);
            var partOem = Normalize(part.OEMNumber);
            if (!string.IsNullOrWhiteSpace(requestedOem)
                && !string.IsNullOrWhiteSpace(partOem)
                && (requestedOem.Contains(partOem, StringComparison.OrdinalIgnoreCase)
                    || partOem.Contains(requestedOem, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var requestedTokens = Normalize(request.RequestedPartName)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => token.Length >= 3)
                .ToArray();
            if (requestedTokens.Length == 0)
            {
                return false;
            }

            var haystack = Normalize(string.Join(" ", new[]
            {
                part.InternalCode,
                part.Name,
                part.OEMNumber,
                part.Notes
            }.Where(value => !string.IsNullOrWhiteSpace(value))));
            var matches = requestedTokens.Count(token => haystack.Contains(token, StringComparison.OrdinalIgnoreCase));
            return matches >= Math.Min(2, requestedTokens.Length);
        }

        private static bool IsMarginWatch(PartDto part)
        {
            if (part.SalePrice <= 0m)
            {
                return true;
            }

            if (part.CostPrice > 0m && (part.SalePrice - part.CostPrice) / part.SalePrice < 0.22m)
            {
                return true;
            }

            var market = part.EstimatedMarketPrice.GetValueOrDefault() > 0m
                ? part.EstimatedMarketPrice.GetValueOrDefault()
                : part.AveragePrice.GetValueOrDefault();
            return market > 0m && part.SalePrice < market * 0.9m;
        }

        private int GetPartActionScore(PartDto part)
        {
            var matches = GetDemandMatchCount(part);
            if (matches > 0 && part.AvailableQuantity <= 0 && part.ReservedQuantity <= 0)
            {
                return 30 + matches * 5;
            }

            if (matches > 0 && part.AvailableQuantity > 0)
            {
                return 24 + matches * 5;
            }

            if (part.AvailableQuantity <= part.MinStock)
            {
                return 14;
            }

            return IsMarginWatch(part) ? 10 : 0;
        }

        private string GetPartActionLabel(PartDto part)
        {
            var matches = GetDemandMatchCount(part);
            if (matches > 0 && part.AvailableQuantity <= 0 && part.ReservedQuantity <= 0)
            {
                return $"Source now: {matches:N0} active demand match{(matches == 1 ? string.Empty : "es")} without ready stock.";
            }

            if (matches > 0 && part.AvailableQuantity > 0)
            {
                return $"Quote now: {matches:N0} demand match{(matches == 1 ? string.Empty : "es")} can be served from stock.";
            }

            if (part.AvailableQuantity <= part.MinStock)
            {
                return $"Reorder soon: available stock is at or below minimum ({part.MinStock:N0}).";
            }

            if (IsMarginWatch(part))
            {
                return "Margin watch: price is missing, below market, or too close to cost.";
            }

            return "Ready: no urgent demand, source, stock, or margin signal.";
        }

        private static bool MatchesPartFilter(PartDto part, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var haystack = Normalize(string.Join(" ", new[]
            {
                part.InternalCode,
                part.Barcode,
                part.Name,
                part.OEMNumber,
                part.Condition.ToString(),
                part.Currency,
                part.Notes,
                part.UsedCarId?.ToString(CultureInfo.InvariantCulture)
            }.Where(value => !string.IsNullOrWhiteSpace(value))));

            return haystack.Contains(query, StringComparison.OrdinalIgnoreCase);
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

        private void RaisePartInventorySummaries()
        {
            OnPropertyChanged(nameof(TotalPartCount));
            OnPropertyChanged(nameof(FilteredPartCount));
            OnPropertyChanged(nameof(AvailablePartCount));
            OnPropertyChanged(nameof(ReservedPartCount));
            OnPropertyChanged(nameof(SoldPartCount));
            OnPropertyChanged(nameof(LowStockPartCount));
            OnPropertyChanged(nameof(DonorPartCount));
            OnPropertyChanged(nameof(DemandMatchPartCount));
            OnPropertyChanged(nameof(ActionNeededPartCount));
            OnPropertyChanged(nameof(MarginWatchPartCount));
            OnPropertyChanged(nameof(InventoryBoardSummary));
            OnPropertyChanged(nameof(TodayFocusSummary));
            OnPropertyChanged(nameof(SelectedPartStockSummary));
            OnPropertyChanged(nameof(SelectedPartPriceSummary));
            OnPropertyChanged(nameof(SelectedPartDonorSummary));
            OnPropertyChanged(nameof(SelectedPartDemandSummary));
            OnPropertyChanged(nameof(SelectedPartActionSummary));
        }

        private void UpdatePricingCoach()
        {
            var averagePrice = SmartPricingCoach.ParseAveragePrice(NewPartEstimatedMarketPrice)
                ?? SmartPricingCoach.ParseAveragePrice(NewPartAveragePrice);
            var availableQuantity = SelectedPart?.AvailableQuantity ?? SelectedPart?.StockQuantity ?? 0;
            _pricingCoach = SmartPricingCoach.Evaluate(
                NewPartCostPrice,
                NewPartSalePrice,
                averagePrice,
                NewPartCurrency,
                availableQuantity,
                NewPartMinStock);
            OnPropertyChanged(nameof(SmartPricingCoachMessage));
            OnPropertyChanged(nameof(SmartPricingCoachBadge));
            OnPropertyChanged(nameof(SmartPricingCoachTone));
        }
    }
}
