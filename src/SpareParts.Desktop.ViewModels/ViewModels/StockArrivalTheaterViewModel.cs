using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
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
    public sealed class StockArrivalTheaterViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private readonly Action<AppScreen>? _navigate;
        private readonly List<StockArrivalOpportunity> _allOpportunities = new();

        private StockArrivalOpportunity? _selectedOpportunity;
        private string _filterText = string.Empty;
        private string _status = "Load purchase and used-car arrivals to find the next selling opportunity.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;

        public StockArrivalTheaterViewModel(ICrudApiClient crudApi, Action<AppScreen>? navigate = null)
        {
            _crudApi = crudApi;
            _navigate = navigate;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshCommand = LoadCommand;
            OpenSelectedWorkspaceCommand = new RelayCommand(_ => OpenSelectedWorkspace());

            Lanes.Add(new StockArrivalLane("photo", "Photograph", "Parts and cars that should be shot before selling.", Brushes.DeepSkyBlue));
            Lanes.Add(new StockArrivalLane("waiting", "Waiting Customers", "Demand that can be contacted now.", Brushes.LimeGreen));
            Lanes.Add(new StockArrivalLane("campaign", "Campaigns", "Ready-made audiences and arrival bundles.", Brushes.Orange));
            Lanes.Add(new StockArrivalLane("pricing", "Price", "Stock that needs a quote before it can move.", Brushes.Gold));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<StockArrivalLane> Lanes { get; } = new();
        public ObservableCollection<StockArrivalMetricTile> MetricTiles { get; } = new();
        public ObservableCollection<StockArrivalFeedRow> ArrivalFeed { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand OpenSelectedWorkspaceCommand { get; }

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
                RefreshVisibleLanes();
            }
        }

        public StockArrivalOpportunity? SelectedOpportunity
        {
            get => _selectedOpportunity;
            set
            {
                if (ReferenceEquals(_selectedOpportunity, value))
                {
                    return;
                }

                _selectedOpportunity = value;
                OnPropertyChanged(nameof(SelectedOpportunity));
                OnPropertyChanged(nameof(SelectedTitle));
                OnPropertyChanged(nameof(SelectedSubtitle));
                OnPropertyChanged(nameof(SelectedAction));
                OnPropertyChanged(nameof(SelectedMetric));
                OnPropertyChanged(nameof(OpenSelectedWorkspaceLabel));
                OnPropertyChanged(nameof(CanOpenSelectedWorkspace));
            }
        }

        public string SelectedTitle => SelectedOpportunity?.Title ?? "Select an opportunity";

        public string SelectedSubtitle => SelectedOpportunity?.Subtitle ?? "Choose a card to see its live data and next move.";

        public string SelectedAction => SelectedOpportunity?.ActionLabel ?? "No action selected.";

        public string SelectedMetric => SelectedOpportunity?.Metric ?? string.Empty;

        public string OpenSelectedWorkspaceLabel => SelectedOpportunity?.TargetLabel ?? "Open workspace";

        public bool CanOpenSelectedWorkspace => SelectedOpportunity?.TargetScreen.HasValue == true;

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
            Status = "Loading stock arrival theater...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var partsTask = LoadSourceAsync<PartDto>("/api/parts?page=1&pageSize=5000");
                var requestsTask = LoadSourceAsync<PartRequestDto>("/api/partrequests?status=Active");
                var usedCarsTask = LoadSourceAsync<UsedCarDto>("/api/usedcars");
                var assetsTask = LoadSourceAsync<WhatsAppCampaignAssetDto>("/api/communications/campaign-assets");
                var purchasesTask = LoadSourceAsync<PurchaseInvoiceLookupDto>("/api/purchases");
                var usedCarPurchasesTask = LoadSourceAsync<UsedCarPurchaseSummaryDto>("/api/purchases/used-cars");

                await Task.WhenAll(partsTask, requestsTask, usedCarsTask, assetsTask, purchasesTask, usedCarPurchasesTask);

                var parts = partsTask.Result.Rows;
                var requests = requestsTask.Result.Rows;
                var usedCars = usedCarsTask.Result.Rows;
                var assets = assetsTask.Result.Rows;
                var purchases = purchasesTask.Result.Rows;
                var usedCarPurchases = usedCarPurchasesTask.Result.Rows;
                var unavailableSources = new[]
                {
                    partsTask.Result.Succeeded,
                    requestsTask.Result.Succeeded,
                    usedCarsTask.Result.Succeeded,
                    assetsTask.Result.Succeeded,
                    purchasesTask.Result.Succeeded,
                    usedCarPurchasesTask.Result.Succeeded
                }.Count(succeeded => !succeeded);

                RebuildBoard(parts, requests, usedCars, assets);
                RebuildMetrics(parts, requests, assets, purchases, usedCarPurchases);
                RebuildArrivalFeed(purchases, usedCarPurchases);
                RefreshVisibleLanes(selectFirstWhenMissing: true);

                Status = unavailableSources > 0
                    ? $"Stock arrival theater loaded with {unavailableSources:N0} source(s) unavailable."
                    : "Stock arrival theater loaded.";
                StatusBrush = unavailableSources > 0 ? Brushes.Khaki : Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                Status = "Could not load stock arrival theater.";
                StatusBrush = Brushes.IndianRed;
                AppNotificationCenter.Instance.Publish($"Stock arrival theater failed: {ex.Message}", false);
                _allOpportunities.Clear();
                foreach (var lane in Lanes)
                {
                    lane.Replace(Array.Empty<StockArrivalOpportunity>());
                }
                MetricTiles.Clear();
                ArrivalFeed.Clear();
                SelectedOpportunity = null;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<StockArrivalSourceResult<T>> LoadSourceAsync<T>(string url)
        {
            try
            {
                return new StockArrivalSourceResult<T>(await _crudApi.GetAllAsync<T>(url), true);
            }
            catch
            {
                return new StockArrivalSourceResult<T>(new List<T>(), false);
            }
        }

        private void RebuildBoard(
            IReadOnlyList<PartDto> parts,
            IReadOnlyList<PartRequestDto> requests,
            IReadOnlyList<UsedCarDto> usedCars,
            IReadOnlyList<WhatsAppCampaignAssetDto> assets)
        {
            _allOpportunities.Clear();
            _allOpportunities.AddRange(BuildPhotoOpportunities(parts, usedCars, assets));
            _allOpportunities.AddRange(BuildWaitingOpportunities(requests));
            _allOpportunities.AddRange(BuildCampaignOpportunities(parts, requests, usedCars, assets));
            _allOpportunities.AddRange(BuildPricingOpportunities(parts, requests));
        }

        private void RebuildMetrics(
            IReadOnlyList<PartDto> parts,
            IReadOnlyList<PartRequestDto> requests,
            IReadOnlyList<WhatsAppCampaignAssetDto> assets,
            IReadOnlyList<PurchaseInvoiceLookupDto> purchases,
            IReadOnlyList<UsedCarPurchaseSummaryDto> usedCarPurchases)
        {
            var readyRequests = requests.Where(IsReadyRequest).ToList();
            var waitingCustomers = readyRequests.Sum(request => Math.Max(1, request.WaitingCustomerCount));
            var campaignCount = _allOpportunities.Count(item => item.LaneKey == "campaign");
            var pricedStock = parts.Count(part => AvailableQuantity(part) > 0 && part.SalePrice > 0);
            var arrivalCount = purchases.Count + usedCarPurchases.Count;

            Replace(MetricTiles, new[]
            {
                new StockArrivalMetricTile("Recent arrivals", arrivalCount.ToString("N0", CultureInfo.CurrentCulture), "Purchases and cars", Brushes.DeepSkyBlue),
                new StockArrivalMetricTile("Ready requests", readyRequests.Count.ToString("N0", CultureInfo.CurrentCulture), $"{waitingCustomers:N0} waiting customers", Brushes.LimeGreen),
                new StockArrivalMetricTile("Campaigns", campaignCount.ToString("N0", CultureInfo.CurrentCulture), assets.Count == 0 ? "Campaign assets unavailable" : "Suggested sends", Brushes.Orange),
                new StockArrivalMetricTile("Priced stock", pricedStock.ToString("N0", CultureInfo.CurrentCulture), "Available to sell", Brushes.Gold)
            });
        }

        private void RebuildArrivalFeed(
            IReadOnlyList<PurchaseInvoiceLookupDto> purchases,
            IReadOnlyList<UsedCarPurchaseSummaryDto> usedCarPurchases)
        {
            var feed = purchases
                .Select(purchase => new StockArrivalFeedRow(
                    "Part purchase",
                    string.IsNullOrWhiteSpace(purchase.PurchaseNumber) ? $"Purchase #{purchase.PurchaseId}" : purchase.PurchaseNumber,
                    $"{Money(purchase.TotalAmount, "USD")} / {ShortDate(purchase.PurchaseDate)}",
                    purchase.PurchaseDate))
                .Concat(usedCarPurchases.Select(purchase => new StockArrivalFeedRow(
                    "Used car",
                    string.IsNullOrWhiteSpace(purchase.PurchaseNumber) ? $"Vehicle purchase #{purchase.Id}" : purchase.PurchaseNumber,
                    $"{purchase.UsedCar} / {ShortDate(purchase.PurchaseDate)}",
                    purchase.PurchaseDate)))
                .OrderByDescending(row => row.Date)
                .Take(5)
                .ToList();

            Replace(ArrivalFeed, feed);
        }

        private IEnumerable<StockArrivalOpportunity> BuildPhotoOpportunities(
            IReadOnlyList<PartDto> parts,
            IReadOnlyList<UsedCarDto> usedCars,
            IReadOnlyList<WhatsAppCampaignAssetDto> assets)
        {
            var carAssetIds = assets
                .Where(asset => string.Equals(asset.AssetType, "Car", StringComparison.OrdinalIgnoreCase))
                .Select(asset => asset.Id)
                .ToHashSet();

            var carCards = assets
                .Where(asset => string.Equals(asset.AssetType, "Car", StringComparison.OrdinalIgnoreCase))
                .OrderBy(asset => asset.ImageCount)
                .Select(asset => StockArrivalOpportunity.Create(
                    "photo",
                    "Used car",
                    string.IsNullOrWhiteSpace(asset.Title) ? $"Used car #{asset.Id}" : asset.Title,
                    string.IsNullOrWhiteSpace(asset.Subtitle) ? "Add showcase photos" : asset.Subtitle,
                    $"{asset.ImageCount:N0} photos",
                    asset.ImageCount > 0 ? "Review gallery" : "Shoot gallery",
                    AppScreen.RepairPrepBoard,
                    "Open prep",
                    Brushes.DeepSkyBlue,
                    ("Asset", "Vehicle arrival"),
                    ("Campaign status", asset.ImageCount > 0 ? "Can be promoted" : "Needs photos"),
                    ("Price", asset.Price.HasValue ? Money(asset.Price.Value, asset.Currency ?? "USD") : "-")))
                .Concat(usedCars
                    .Where(car => !carAssetIds.Contains(car.Id))
                    .OrderByDescending(car => car.IsReceived)
                    .ThenByDescending(car => car.ModelYear)
                    .Select(car => StockArrivalOpportunity.Create(
                        "photo",
                        "Used car",
                        CarTitle(car),
                        JoinNonEmpty(car.Barcode, car.Location, car.SupplierName, fallback: "Vehicle arrival"),
                        car.IsReceived ? "Received" : "Incoming",
                        car.IsReceived ? "Shoot gallery" : "Track arrival",
                        AppScreen.RepairPrepBoard,
                        "Open prep",
                        Brushes.DeepSkyBlue,
                        ("Status", car.IsReceived ? "Received" : "Incoming"),
                        ("Location", string.IsNullOrWhiteSpace(car.Location) ? "-" : car.Location),
                        ("Cost", Money(car.FullCostBase > 0 ? car.FullCostBase : car.Price, car.BaseCurrencyCode)))))
                .Take(7)
                .ToList();

            var partCards = parts
                .Where(part => AvailableQuantity(part) > 0)
                .OrderByDescending(part => part.UsedCarId.HasValue)
                .ThenByDescending(AvailableQuantity)
                .Take(Math.Max(0, 8 - carCards.Count))
                .Select(part => StockArrivalOpportunity.Create(
                    "photo",
                    part.UsedCarId.HasValue ? "Donor part" : "Part",
                    PartTitle(part),
                    string.IsNullOrWhiteSpace(part.OEMNumber) ? "Ready for product photos" : $"OEM {part.OEMNumber}",
                    $"{AvailableQuantity(part):N0} in stock",
                    "Photograph",
                    AppScreen.StockManagement,
                    "Open stock",
                    Brushes.DeepSkyBlue,
                    ("Stock", AvailableQuantity(part).ToString("N0", CultureInfo.CurrentCulture)),
                    ("Current price", Money(part.SalePrice, part.Currency)),
                    ("Source", part.UsedCarId.HasValue ? $"Used car #{part.UsedCarId.Value}" : "Parts inventory")));

            return carCards.Concat(partCards).Take(8);
        }

        private IEnumerable<StockArrivalOpportunity> BuildWaitingOpportunities(IReadOnlyList<PartRequestDto> requests)
        {
            return requests
                .Where(IsReadyRequest)
                .OrderByDescending(request => Math.Max(1, request.WaitingCustomerCount))
                .ThenByDescending(request => request.CreatedAt)
                .Take(10)
                .Select(request => StockArrivalOpportunity.Create(
                    "waiting",
                    $"{Math.Max(1, request.WaitingCustomerCount):N0} waiting",
                    string.IsNullOrWhiteSpace(request.MatchedPartName) ? request.RequestedPartName : request.MatchedPartName,
                    JoinNonEmpty(request.CustomerName, request.VehicleDetails, fallback: "Customer request"),
                    string.IsNullOrWhiteSpace(request.PartInternalCode) ? request.RequestedOemNumber ?? "Match stock" : request.PartInternalCode,
                    "Contact",
                    AppScreen.WhatsAppInbox,
                    "Open WhatsApp",
                    Brushes.LimeGreen,
                    ("Customer", request.CustomerName),
                    ("Phone", request.CustomerPhone ?? "-"),
                    ("Available", request.AvailableQuantity.ToString("N0", CultureInfo.CurrentCulture)),
                    ("Status", request.Status)));
        }

        private IEnumerable<StockArrivalOpportunity> BuildCampaignOpportunities(
            IReadOnlyList<PartDto> parts,
            IReadOnlyList<PartRequestDto> requests,
            IReadOnlyList<UsedCarDto> usedCars,
            IReadOnlyList<WhatsAppCampaignAssetDto> assets)
        {
            var readyRequests = requests.Where(IsReadyRequest).ToList();
            var waitingPartIds = readyRequests
                .Where(request => request.PartId.HasValue)
                .Select(request => request.PartId!.Value)
                .Distinct()
                .ToList();
            var stockedPartIds = parts
                .Where(part => AvailableQuantity(part) > 0 && part.SalePrice > 0)
                .Select(part => part.Id)
                .Distinct()
                .Take(12)
                .ToList();
            var photographedCarIds = assets
                .Where(asset => string.Equals(asset.AssetType, "Car", StringComparison.OrdinalIgnoreCase) && asset.ImageCount > 0)
                .Select(asset => asset.Id)
                .Concat(usedCars.Where(car => car.IsReceived).Select(car => car.Id))
                .Distinct()
                .Take(8)
                .ToList();
            var readyPartIds = waitingPartIds.Count > 0 ? waitingPartIds : stockedPartIds;

            if (readyRequests.Count > 0)
            {
                yield return StockArrivalOpportunity.Create(
                    "campaign",
                    "Waiting parts",
                    "Back-in-stock message",
                    $"{readyRequests.Count:N0} matched request(s) can be contacted.",
                    $"{readyRequests.Sum(request => Math.Max(1, request.WaitingCustomerCount)):N0} customers",
                    "Preview and send",
                    AppScreen.WhatsAppInbox,
                    "Open WhatsApp",
                    Brushes.Orange,
                    ("Audience", "WaitingForParts"),
                    ("Matched parts", readyPartIds.Count.ToString("N0", CultureInfo.CurrentCulture)),
                    ("Requests", readyRequests.Count.ToString("N0", CultureInfo.CurrentCulture)));
            }

            if (stockedPartIds.Count > 0)
            {
                yield return StockArrivalOpportunity.Create(
                    "campaign",
                    "Fresh arrivals",
                    "New stock campaign",
                    "Promote recently available priced parts to active customers.",
                    $"{stockedPartIds.Count:N0} parts",
                    "Preview and send",
                    AppScreen.WhatsAppInbox,
                    "Open WhatsApp",
                    Brushes.Orange,
                    ("Audience", "ActiveCustomers"),
                    ("Parts", stockedPartIds.Count.ToString("N0", CultureInfo.CurrentCulture)),
                    ("Ready", "Priced stock"));
            }

            if (photographedCarIds.Count > 0)
            {
                yield return StockArrivalOpportunity.Create(
                    "campaign",
                    "Used cars",
                    "Vehicle arrivals showcase",
                    "Cars with images can anchor a WhatsApp showcase.",
                    $"{photographedCarIds.Count:N0} cars",
                    "Preview and send",
                    AppScreen.WhatsAppInbox,
                    "Open WhatsApp",
                    Brushes.Orange,
                    ("Audience", "AllCustomers"),
                    ("Cars", photographedCarIds.Count.ToString("N0", CultureInfo.CurrentCulture)),
                    ("Ready", "Photos attached"));
            }
        }

        private IEnumerable<StockArrivalOpportunity> BuildPricingOpportunities(
            IReadOnlyList<PartDto> parts,
            IReadOnlyList<PartRequestDto> requests)
        {
            var waitingByPart = BuildWaitingByPart(requests);
            return parts
                .Where(part => AvailableQuantity(part) > 0)
                .Select(part => new
                {
                    Part = part,
                    Waiting = waitingByPart.TryGetValue(part.Id, out var waiting) ? waiting : 0,
                    Signal = PricingSignal(part, waitingByPart.TryGetValue(part.Id, out var count) ? count : 0)
                })
                .Where(item => item.Signal.ShouldShow)
                .OrderBy(item => item.Part.SalePrice > 0 ? 1 : 0)
                .ThenByDescending(item => item.Waiting)
                .ThenByDescending(item => item.Signal.SuggestedPrice)
                .Take(10)
                .Select(item => StockArrivalOpportunity.Create(
                    "pricing",
                    item.Signal.Badge,
                    PartTitle(item.Part),
                    item.Signal.Message,
                    item.Part.SalePrice > 0 ? Money(item.Part.SalePrice, item.Part.Currency) : "No sale price",
                    "Set price",
                    AppScreen.StockManagement,
                    "Open stock",
                    Brushes.Gold,
                    ("Stock", AvailableQuantity(item.Part).ToString("N0", CultureInfo.CurrentCulture)),
                    ("Waiting", item.Waiting.ToString("N0", CultureInfo.CurrentCulture)),
                    ("Suggested", item.Signal.SuggestedPrice > 0 ? Money(item.Signal.SuggestedPrice, item.Part.Currency) : "-"),
                    ("Cost", Money(item.Part.CostPrice, item.Part.Currency))));
        }

        private void RefreshVisibleLanes(bool selectFirstWhenMissing = false)
        {
            var needle = FilterText.Trim().ToLowerInvariant();
            foreach (var lane in Lanes)
            {
                var rows = _allOpportunities
                    .Where(item => string.Equals(item.LaneKey, lane.Key, StringComparison.Ordinal))
                    .Where(item => string.IsNullOrWhiteSpace(needle) || item.SearchText.Contains(needle, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                lane.Replace(rows);
            }

            if (selectFirstWhenMissing && (SelectedOpportunity == null || !_allOpportunities.Contains(SelectedOpportunity)))
            {
                SelectedOpportunity = _allOpportunities.FirstOrDefault();
            }
            else if (!selectFirstWhenMissing && SelectedOpportunity != null && !Lanes.SelectMany(lane => lane.Opportunities).Contains(SelectedOpportunity))
            {
                SelectedOpportunity = Lanes.SelectMany(lane => lane.Opportunities).FirstOrDefault();
            }
        }

        private void OpenSelectedWorkspace()
        {
            if (SelectedOpportunity?.TargetScreen is not AppScreen screen)
            {
                return;
            }

            _navigate?.Invoke(screen);
        }

        private void HandleBackgroundException(Exception ex)
            => AppNotificationCenter.Instance.Publish($"Stock arrival theater failed: {ex.Message}", false);

        private static bool IsReadyRequest(PartRequestDto request)
            => request.IsReadyToContact
               || (IsActiveRequest(request) && request.AvailableQuantity > 0);

        private static bool IsActiveRequest(PartRequestDto request)
            => string.Equals(request.Status, PartRequestStatus.Open, StringComparison.OrdinalIgnoreCase)
               || string.Equals(request.Status, PartRequestStatus.Contacted, StringComparison.OrdinalIgnoreCase);

        private static Dictionary<int, int> BuildWaitingByPart(IEnumerable<PartRequestDto> requests)
        {
            return requests
                .Where(request => request.PartId.HasValue && IsActiveRequest(request))
                .GroupBy(request => request.PartId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => Math.Max(
                        group.Select(CustomerKey).Distinct().Count(),
                        group.Max(request => request.WaitingCustomerCount)));
        }

        private static string CustomerKey(PartRequestDto request)
            => request.CustomerId.HasValue
                ? $"id:{request.CustomerId.Value.ToString(CultureInfo.InvariantCulture)}"
                : $"{request.CustomerName.Trim().ToUpperInvariant()}|{request.CustomerPhone}";

        private static StockArrivalPricingSignal PricingSignal(PartDto part, int waiting)
        {
            var available = AvailableQuantity(part);
            var sale = part.SalePrice;
            var cost = part.CostPrice;
            var average = part.AveragePrice ?? 0m;
            var suggested = SuggestedPrice(cost, sale, average, waiting, available <= Math.Max(1, part.MinStock));
            var marginRate = sale > 0 ? (sale - cost) / sale : 0m;

            if (sale <= 0)
            {
                return new StockArrivalPricingSignal(true, "Set price", "Add a sale price before quoting this arrival.", suggested);
            }

            if (waiting > 0 && suggested > sale)
            {
                return new StockArrivalPricingSignal(true, $"{waiting:N0} waiting", $"Demand is live. Suggested price: {Money(suggested, part.Currency)}.", suggested);
            }

            if (average > 0 && sale < average * 0.9m)
            {
                return new StockArrivalPricingSignal(true, "Below usual", $"Current price is below the usual {Money(average, part.Currency)} market range.", suggested);
            }

            if (cost > 0 && marginRate < 0.22m)
            {
                return new StockArrivalPricingSignal(true, "Low margin", $"Margin is under 22%. Suggested price: {Money(suggested, part.Currency)}.", suggested);
            }

            return new StockArrivalPricingSignal(false, "Price ok", "Price is ready.", suggested);
        }

        private static decimal SuggestedPrice(decimal cost, decimal sale, decimal average, int waiting, bool rare)
        {
            var marginFloor = cost > 0 ? cost / 0.72m : 0m;
            var marketAnchor = average > 0 ? average : sale;
            var demandLift = rare && waiting > 0
                ? Math.Min(1.3m, 1.12m + waiting * 0.045m)
                : 1m;
            var suggested = Math.Max(Math.Max(marketAnchor * demandLift, marginFloor), sale);
            return RoundToNearestFive(suggested);
        }

        private static decimal RoundToNearestFive(decimal value)
            => value <= 0 ? 0 : Math.Max(5m, Math.Round(value / 5m, MidpointRounding.AwayFromZero) * 5m);

        private static decimal AvailableQuantity(PartDto part)
            => Math.Max(part.AvailableQuantity, part.StockQuantity);

        private static string PartTitle(PartDto part)
            => JoinNonEmpty(part.InternalCode, part.Name, fallback: $"Part #{part.Id}");

        private static string CarTitle(UsedCarDto car)
            => $"{car.Car} {car.ModelYear}".Trim();

        private static string Money(decimal value, string? currency)
            => $"{(string.IsNullOrWhiteSpace(currency) ? "USD" : currency)} {value:N2}";

        private static string ShortDate(DateTime value)
            => value == default ? "-" : value.ToString("MMM d, yyyy", CultureInfo.CurrentCulture);

        private static string JoinNonEmpty(string? first = null, string? second = null, string? third = null, string fallback = "-")
        {
            var joined = string.Join(" / ", new[] { first, second, third }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrWhiteSpace(joined) ? fallback : joined;
        }

        private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> values)
        {
            collection.Clear();
            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class StockArrivalLane : INotifyPropertyChanged
    {
        public StockArrivalLane(string key, string title, string subtitle, Brush accentBrush)
        {
            Key = key;
            Title = title;
            Subtitle = subtitle;
            AccentBrush = accentBrush;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Key { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public Brush AccentBrush { get; }
        public ObservableCollection<StockArrivalOpportunity> Opportunities { get; } = new();
        public int Count => Opportunities.Count;

        public void Replace(IEnumerable<StockArrivalOpportunity> rows)
        {
            Opportunities.Clear();
            foreach (var row in rows)
            {
                Opportunities.Add(row);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }
    }

    public sealed class StockArrivalOpportunity
    {
        private StockArrivalOpportunity(
            string laneKey,
            string type,
            string title,
            string subtitle,
            string metric,
            string actionLabel,
            AppScreen? targetScreen,
            string targetLabel,
            Brush accentBrush,
            IEnumerable<StockArrivalEvidenceRow> evidence)
        {
            LaneKey = laneKey;
            Type = type;
            Title = title;
            Subtitle = subtitle;
            Metric = metric;
            ActionLabel = actionLabel;
            TargetScreen = targetScreen;
            TargetLabel = targetLabel;
            AccentBrush = accentBrush;
            Evidence = new ObservableCollection<StockArrivalEvidenceRow>(evidence);
            SearchText = $"{type} {title} {subtitle} {metric}".ToLowerInvariant();
        }

        public string LaneKey { get; }
        public string Type { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Metric { get; }
        public string ActionLabel { get; }
        public AppScreen? TargetScreen { get; }
        public string TargetLabel { get; }
        public Brush AccentBrush { get; }
        public ObservableCollection<StockArrivalEvidenceRow> Evidence { get; }
        public string SearchText { get; }

        public static StockArrivalOpportunity Create(
            string laneKey,
            string type,
            string title,
            string subtitle,
            string metric,
            string actionLabel,
            AppScreen? targetScreen,
            string targetLabel,
            Brush accentBrush,
            params (string Label, string Value)[] evidence)
            => new(
                laneKey,
                type,
                title,
                subtitle,
                metric,
                actionLabel,
                targetScreen,
                targetLabel,
                accentBrush,
                evidence.Select(item => new StockArrivalEvidenceRow(item.Label, item.Value)));
    }

    public sealed record StockArrivalEvidenceRow(string Label, string Value);

    public sealed record StockArrivalMetricTile(string Label, string Value, string Detail, Brush AccentBrush);

    public sealed record StockArrivalFeedRow(string Type, string Title, string Detail, DateTime Date);

    internal sealed record StockArrivalPricingSignal(bool ShouldShow, string Badge, string Message, decimal SuggestedPrice);

    internal sealed record StockArrivalSourceResult<T>(List<T> Rows, bool Succeeded);
}
