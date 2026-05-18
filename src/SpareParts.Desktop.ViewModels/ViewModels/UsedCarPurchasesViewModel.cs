using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
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
    public sealed class UsedCarPurchasesViewModel : INotifyPropertyChanged
    {
        private const string UsedCarPriceRoleKey = "used_car_price";
        private const string UsedCarTransportationRoleKey = "used_car_transportation";
        private const string UsedCarPartOutRoleKey = "used_car_partout";
        private const string UsedCarShippingRoleKey = "used_car_shipping";
        private const string UsedCarCustomsRoleKey = "used_car_customs";
        private const string UsedCarRepairsRoleKey = "used_car_repairs";

        private readonly ICrudApiClient _crudApi;
        private readonly IAccountingApiClient _accountingApi;
        private readonly IPurchasesApiClient _purchasesApi;
        private readonly Dictionary<string, int?> _defaultAccountsByRole = new(StringComparer.OrdinalIgnoreCase);
        private string _baseCurrencyCode = "USD";
        private string _counterCurrencyCode = "USD";

        private int? _selectedUsedCarId;
        private int? _selectedSupplierId;
        private DateTime _purchaseDate = DateTime.Today;
        private decimal _paidAmount;
        private string _paidAmountText = "0";
        private string _notes = string.Empty;
        private bool _isLoading;
        private string _status = "Used-car purchases are ready.";
        private Brush _statusBrush = Brushes.LightGreen;
        private UsedCarPurchaseSummaryDto? _selectedRecentPurchase;
        private UsedCarPurchaseDetailDto? _selectedPurchaseDetail;
        private int _selectedPurchaseDetailVersion;

        public ObservableCollection<UsedCarDto> UsedCars { get; } = new();
        public ObservableCollection<SupplierDto> Suppliers { get; } = new();
        public ObservableCollection<AccountDto> Accounts { get; } = new();
        public ObservableCollection<PurchaseAccountOption> AccountOptions { get; } = new();
        public ObservableCollection<UsedCarPurchaseLineViewModel> PurchaseLines { get; } = new();
        public ObservableCollection<UsedCarPurchaseSummaryDto> RecentPurchases { get; } = new();

        public int? SelectedUsedCarId
        {
            get => _selectedUsedCarId;
            set
            {
                if (_selectedUsedCarId == value) return;
                _selectedUsedCarId = value;
                OnPropertyChanged(nameof(SelectedUsedCarId));
                OnPropertyChanged(nameof(SelectedUsedCar));
                OnPropertyChanged(nameof(BaseCurrencyCode));
                OnPropertyChanged(nameof(CounterCurrencyCode));
                OnPropertyChanged(nameof(TotalBaseLabel));
                OnPropertyChanged(nameof(TotalCounterLabel));
                OnPropertyChanged(nameof(PaidAmountLabel));
                OnPropertyChanged(nameof(RemainingCounterLabel));
                OnPropertyChanged(nameof(PaidAmountBase));
                GeneratePurchaseLines();
            }
        }

        public int? SelectedSupplierId
        {
            get => _selectedSupplierId;
            set
            {
                if (_selectedSupplierId == value) return;
                _selectedSupplierId = value;
                OnPropertyChanged(nameof(SelectedSupplierId));
            }
        }

        public DateTime PurchaseDate
        {
            get => _purchaseDate;
            set
            {
                if (_purchaseDate == value) return;
                _purchaseDate = value;
                OnPropertyChanged(nameof(PurchaseDate));
            }
        }

        public string PaidAmountText
        {
            get => _paidAmountText;
            set
            {
                if (_paidAmountText == value) return;
                _paidAmountText = value;
                OnPropertyChanged(nameof(PaidAmountText));

                if (TryParseEditableAmount(value, out var parsedAmount))
                {
                    SetPaidAmount(parsedAmount, synchronizeText: false);
                }
            }
        }

        public decimal PaidAmount
        {
            get => _paidAmount;
            set => SetPaidAmount(value, synchronizeText: true);
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes == value) return;
                _notes = value;
                OnPropertyChanged(nameof(Notes));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value) return;
                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public UsedCarPurchaseSummaryDto? SelectedRecentPurchase
        {
            get => _selectedRecentPurchase;
            set
            {
                if (ReferenceEquals(_selectedRecentPurchase, value)) return;
                _selectedRecentPurchase = value;
                OnPropertyChanged(nameof(SelectedRecentPurchase));
                OnPropertyChanged(nameof(CanDeleteSelectedPurchase));
                OnPropertyChanged(nameof(CanPostSelectedPurchase));
                LoadSelectedPurchaseDetailAsync(value?.Id).SafeFireAndForget(HandleBackgroundException);
            }
        }

        public UsedCarPurchaseDetailDto? SelectedPurchaseDetail
        {
            get => _selectedPurchaseDetail;
            private set
            {
                if (ReferenceEquals(_selectedPurchaseDetail, value)) return;
                _selectedPurchaseDetail = value;
                OnPropertyChanged(nameof(SelectedPurchaseDetail));
                OnPropertyChanged(nameof(SelectedPurchaseDetailSummary));
                OnPropertyChanged(nameof(SelectedPurchaseNotes));
                OnPropertyChanged(nameof(HasSelectedPurchaseTimeline));
            }
        }

        public UsedCarDto? SelectedUsedCar => SelectedUsedCarId is int usedCarId
            ? UsedCars.FirstOrDefault(item => item.Id == usedCarId)
            : null;

        public string BaseCurrencyCode => NormalizeCurrencyCode(SelectedUsedCar?.BaseCurrencyCode) ?? _baseCurrencyCode;
        public string CounterCurrencyCode => NormalizeCurrencyCode(SelectedUsedCar?.CounterCurrencyCode) ?? _counterCurrencyCode;
        public string TotalBaseLabel => $"Total Base ({BaseCurrencyCode})";
        public string TotalCounterLabel => $"Total Counter ({CounterCurrencyCode})";
        public string PaidAmountLabel => $"Paid ({CounterCurrencyCode})";
        public string RemainingCounterLabel => $"Remaining ({CounterCurrencyCode})";
        public decimal TotalBaseAmount => decimal.Round(PurchaseLines.Sum(line => line.BaseAmount), 4, MidpointRounding.AwayFromZero);
        public decimal TotalCounterAmount => decimal.Round(PurchaseLines.Sum(line => line.CounterAmount), 4, MidpointRounding.AwayFromZero);
        public decimal PaidAmountBase => ConvertToBaseAmount(PaidAmount, ResolveCounterRateToBase(SelectedUsedCar));
        public decimal RemainingBaseAmount => decimal.Round(Math.Max(TotalBaseAmount - PaidAmountBase, 0m), 4, MidpointRounding.AwayFromZero);
        public decimal RemainingCounterAmount => decimal.Round(Math.Max(TotalCounterAmount - PaidAmount, 0m), 4, MidpointRounding.AwayFromZero);
        public string ScreenSummary
        {
            get
            {
                var postedCount = RecentPurchases.Count(item => string.Equals(item.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase));
                var draftCount = RecentPurchases.Count - postedCount;
                return $"{RecentPurchases.Count} used-car purchase(s): {draftCount} draft, {postedCount} posted.";
            }
        }
        public bool CanDeleteSelectedPurchase => SelectedRecentPurchase is { Id: > 0 };
        public bool CanPostSelectedPurchase => SelectedRecentPurchase is { Id: > 0 } purchase
            && !string.Equals(purchase.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase);
        public string SelectedPurchaseDetailSummary => SelectedPurchaseDetail == null
            ? "Select a saved purchase to load its lines from the purchase table."
            : $"{SelectedPurchaseDetail.Lines.Count} saved line(s) loaded for {SelectedPurchaseDetail.PurchaseNumber}.";
        public string SelectedPurchaseNotes => string.IsNullOrWhiteSpace(SelectedPurchaseDetail?.Notes)
            ? "No notes recorded for the selected purchase."
            : SelectedPurchaseDetail.Notes;
        public bool HasSelectedPurchaseTimeline => SelectedPurchaseDetail?.Timeline.Count > 0;

        public ICommand LoadCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand RefreshLinesCommand { get; }
        public ICommand DeleteSelectedPurchaseCommand { get; }
        public ICommand PostSelectedPurchaseCommand { get; }

        public UsedCarPurchasesViewModel(
            ICrudApiClient crudApi,
            IAccountingApiClient accountingApi,
            IPurchasesApiClient purchasesApi)
        {
            _crudApi = crudApi;
            _accountingApi = accountingApi;
            _purchasesApi = purchasesApi;

            LoadCommand = new RelayCommand(_ => _ = LoadAsync());
            SubmitCommand = new RelayCommand(_ => _ = SubmitAsync());
            ResetCommand = new RelayCommand(_ => ResetForm());
            RefreshLinesCommand = new RelayCommand(_ => GeneratePurchaseLines());
            DeleteSelectedPurchaseCommand = new RelayCommand(_ => _ = DeleteSelectedPurchaseAsync());
            PostSelectedPurchaseCommand = new RelayCommand(_ => _ = PostSelectedPurchaseAsync());
        }

        public async Task LoadAsync(int? purchaseIdToSelect = null)
        {
            IsLoading = true;
            SetStatus("Loading used-car purchase workspace…", true);

            try
            {
                var selectedUsedCarId = SelectedUsedCarId;
                var selectedSupplierId = SelectedSupplierId;
                var selectedRecentPurchaseId = SelectedRecentPurchase?.Id;

                var usedCarsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");
                var suppliersTask = _crudApi.GetAllAsync<SupplierDto>("api/suppliers");
                var accountsTask = _accountingApi.GetAccountsAsync();
                var postingSettingsTask = _accountingApi.GetPostingSettingsAsync();
                var purchasesTask = _purchasesApi.GetUsedCarPurchasesAsync();
                var appConstantsTask = _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");

                await Task.WhenAll(usedCarsTask, suppliersTask, accountsTask, postingSettingsTask, purchasesTask, appConstantsTask);

                Replace(UsedCars, usedCarsTask.Result.OrderByDescending(item => item.Id));
                Replace(Suppliers, suppliersTask.Result.OrderBy(item => item.Name));
                Replace(Accounts, accountsTask.Result.OrderBy(item => item.Code));
                Replace(AccountOptions, Accounts.Select(account => new PurchaseAccountOption
                {
                    Id = account.Id,
                    DisplayName = $"{account.Code} · {account.Name}"
                }));
                Replace(RecentPurchases, purchasesTask.Result);

                _defaultAccountsByRole.Clear();
                foreach (var setting in postingSettingsTask.Result)
                {
                    _defaultAccountsByRole[setting.SettingKey] = setting.AccountId;
                }

                ApplyCurrencyDefaults(appConstantsTask.Result);

                var purchaseIdToPreserve = purchaseIdToSelect ?? selectedRecentPurchaseId;

                SelectedUsedCarId = UsedCars.Any(item => item.Id == selectedUsedCarId)
                    ? selectedUsedCarId
                    : UsedCars.FirstOrDefault()?.Id;

                SelectedSupplierId = Suppliers.Any(item => item.Id == selectedSupplierId)
                    ? selectedSupplierId
                    : SelectedSupplierId ?? Suppliers.FirstOrDefault()?.Id;

                SelectedRecentPurchase = RecentPurchases.FirstOrDefault(item => item.Id == purchaseIdToPreserve);

                OnPropertyChanged(nameof(ScreenSummary));
                GeneratePurchaseLines();
                SetStatus("Used-car purchase workspace loaded.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Loading used-car purchases failed: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteSelectedPurchaseAsync()
        {
            if (SelectedRecentPurchase is not { Id: > 0 } purchase)
            {
                SetStatus("Select a purchase to delete first.", false);
                return;
            }

            try
            {
                await _purchasesApi.DeleteUsedCarPurchaseAsync(purchase.Id);
                AppNotificationCenter.Instance.Publish($"✓ Used-car purchase {purchase.PurchaseNumber} deleted.", true);
                SetStatus($"Used-car purchase {purchase.PurchaseNumber} deleted.", true);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
                SetStatus($"Deleting used-car purchase failed: {ex.Message}", false);
            }
        }

        private async Task PostSelectedPurchaseAsync()
        {
            if (SelectedRecentPurchase is not { Id: > 0 } purchase)
            {
                SetStatus("Select a saved purchase to post first.", false);
                return;
            }

            if (string.Equals(purchase.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus($"Purchase {purchase.PurchaseNumber} is already posted.", false);
                return;
            }

            try
            {
                var result = await _purchasesApi.PostUsedCarPurchaseAsync(purchase.Id);
                AppNotificationCenter.Instance.Publish($"✓ Used-car purchase {result.PurchaseNumber} posted.", true);
                SetStatus($"Used-car purchase {result.PurchaseNumber} posted.", true);
                await LoadAsync(purchase.Id);
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
                SetStatus($"Posting saved purchase failed: {ex.Message}", false);
            }
        }

        private async Task SubmitAsync()
        {
            if (SelectedUsedCar is not { Id: > 0 } usedCar)
            {
                SetStatus("Select a used car first.", false);
                return;
            }

            if (SelectedSupplierId is not int supplierId || supplierId <= 0)
            {
                SetStatus("Select a supplier first.", false);
                return;
            }

            if (PaidAmount < 0)
            {
                SetStatus("Paid amount cannot be negative.", false);
                return;
            }

            if (PurchaseLines.Count == 0)
            {
                SetStatus("The selected used car has no positive amount lines to save.", false);
                return;
            }

            if (PurchaseLines.Any(line => line.AccountId is not > 0))
            {
                SetStatus("Assign an account to every used-car purchase line before posting.", false);
                return;
            }

            try
            {
                var request = new CreateUsedCarPurchaseRequest
                {
                    UsedCarId = usedCar.Id,
                    SupplierId = supplierId,
                    PurchaseDate = PurchaseDate,
                    PaidAmount = PaidAmountBase,
                    PaidCounterAmount = PaidAmount,
                    BaseCurrencyCode = BaseCurrencyCode,
                    CounterCurrencyCode = CounterCurrencyCode,
                    Notes = (Notes ?? string.Empty).Trim(),
                    Lines = PurchaseLines
                        .Where(line => line.BaseAmount > 0m)
                        .Select(line => new CreateUsedCarPurchaseLineRequest
                        {
                            DetailKey = line.RoleKey,
                            Description = line.Description,
                            Amount = line.Amount,
                            CurrencyCode = line.CurrencyCode,
                            RateToBase = line.RateToBase,
                            BaseAmount = line.BaseAmount,
                            CounterAmount = line.CounterAmount,
                            AccountId = line.AccountId ?? 0,
                            SortOrder = line.SortOrder
                        })
                        .ToList()
                };

                var result = await _purchasesApi.CreateUsedCarPurchaseAsync(request);
                AppNotificationCenter.Instance.Publish($"✓ Used-car purchase {result.PurchaseNumber} saved as draft.", true);
                SetStatus($"Used-car purchase {result.PurchaseNumber} saved. Post it from purchase history.", true);

                PaidAmount = 0m;
                Notes = string.Empty;
                await LoadAsync(result.PurchaseId);
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
                SetStatus($"Saving used-car purchase failed: {ex.Message}", false);
            }
        }

        private void ResetForm()
        {
            PurchaseDate = DateTime.Today;
            PaidAmount = 0m;
            Notes = string.Empty;
            GeneratePurchaseLines();
            SetStatus("Used-car purchase draft form reset.", true);
        }

        private void GeneratePurchaseLines()
        {
            PurchaseLines.Clear();

            var usedCar = SelectedUsedCar;
            if (usedCar == null)
            {
                OnPropertyChanged(nameof(TotalBaseAmount));
                OnPropertyChanged(nameof(TotalCounterAmount));
                OnPropertyChanged(nameof(RemainingBaseAmount));
                OnPropertyChanged(nameof(RemainingCounterAmount));
                return;
            }

            var counterRateToBase = ResolveCounterRateToBase(usedCar);
            var priceRateToBase = usedCar.Price > 0m && usedCar.PriceBase > 0m
                ? decimal.Round(usedCar.PriceBase / usedCar.Price, 8, MidpointRounding.AwayFromZero)
                : 1m;
            var priceCounterAmount = usedCar.PriceCounter > 0m
                ? decimal.Round(usedCar.PriceCounter, 4, MidpointRounding.AwayFromZero)
                : ConvertBaseToCounterAmount(usedCar.PriceBase, counterRateToBase);

            AddLine(
                UsedCarPriceRoleKey,
                "Vehicle Price",
                usedCar.Price,
                usedCar.PriceCurrency,
                priceRateToBase,
                usedCar.PriceBase,
                priceCounterAmount);
            AddLine(
                UsedCarTransportationRoleKey,
                "Transportation",
                usedCar.Transportation,
                CounterCurrencyCode,
                counterRateToBase,
                ConvertToBaseAmount(usedCar.Transportation, counterRateToBase),
                usedCar.Transportation);
            AddLine(
                UsedCarPartOutRoleKey,
                "Part-Out",
                usedCar.PartOut,
                CounterCurrencyCode,
                counterRateToBase,
                ConvertToBaseAmount(usedCar.PartOut, counterRateToBase),
                usedCar.PartOut);
            AddLine(
                UsedCarShippingRoleKey,
                "Shipping",
                usedCar.Shipping,
                CounterCurrencyCode,
                counterRateToBase,
                ConvertToBaseAmount(usedCar.Shipping, counterRateToBase),
                usedCar.Shipping);
            AddLine(
                UsedCarCustomsRoleKey,
                "Customs",
                usedCar.Customs,
                CounterCurrencyCode,
                counterRateToBase,
                ConvertToBaseAmount(usedCar.Customs, counterRateToBase),
                usedCar.Customs);
            AddLine(
                UsedCarRepairsRoleKey,
                "Repairs",
                usedCar.Repairs,
                CounterCurrencyCode,
                counterRateToBase,
                ConvertToBaseAmount(usedCar.Repairs, counterRateToBase),
                usedCar.Repairs);

            OnPropertyChanged(nameof(TotalBaseAmount));
            OnPropertyChanged(nameof(TotalCounterAmount));
            OnPropertyChanged(nameof(RemainingBaseAmount));
            OnPropertyChanged(nameof(RemainingCounterAmount));
        }

        private void AddLine(
            string roleKey,
            string description,
            decimal amount,
            string? currencyCode,
            decimal rateToBase,
            decimal baseAmount,
            decimal counterAmount)
        {
            var roundedAmount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
            var roundedBaseAmount = decimal.Round(baseAmount, 4, MidpointRounding.AwayFromZero);
            var roundedCounterAmount = decimal.Round(counterAmount, 4, MidpointRounding.AwayFromZero);
            if (roundedAmount <= 0m && roundedBaseAmount <= 0m && roundedCounterAmount <= 0m)
            {
                return;
            }

            PurchaseLines.Add(new UsedCarPurchaseLineViewModel
            {
                SortOrder = PurchaseLines.Count + 1,
                RoleKey = roleKey,
                Description = description,
                Amount = roundedAmount > 0m ? roundedAmount : roundedCounterAmount,
                CurrencyCode = NormalizeCurrencyCode(currencyCode) ?? CounterCurrencyCode,
                RateToBase = decimal.Round(rateToBase > 0m ? rateToBase : 1m, 8, MidpointRounding.AwayFromZero),
                BaseAmount = roundedBaseAmount,
                CounterAmount = roundedCounterAmount,
                AccountId = ResolveDefaultAccountId(roleKey)
            });
        }

        private void ApplyCurrencyDefaults(IEnumerable<AppConstantDto> constants)
        {
            var byKey = constants.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
            _baseCurrencyCode = ResolveCurrencyCode(byKey, "BaseCurrencyCode")
                ?? ResolveCurrencyCode(byKey, "DefaultCurrencyCode")
                ?? "USD";
            _counterCurrencyCode = ResolveCurrencyCode(byKey, "CounterCurrencyCode")
                ?? _baseCurrencyCode;

            OnPropertyChanged(nameof(BaseCurrencyCode));
            OnPropertyChanged(nameof(CounterCurrencyCode));
            OnPropertyChanged(nameof(TotalBaseLabel));
            OnPropertyChanged(nameof(TotalCounterLabel));
            OnPropertyChanged(nameof(PaidAmountLabel));
            OnPropertyChanged(nameof(RemainingCounterLabel));
            OnPropertyChanged(nameof(PaidAmountBase));
        }

        private int? ResolveDefaultAccountId(string roleKey)
            => _defaultAccountsByRole.TryGetValue(roleKey, out var accountId) ? accountId : null;

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static string? NormalizeCurrencyCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : null;
        }

        private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> constants, string key)
        {
            if (!constants.TryGetValue(key, out var value))
            {
                return null;
            }

            return NormalizeCurrencyCode(value);
        }

        private void SetPaidAmount(decimal value, bool synchronizeText)
        {
            var roundedValue = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
            var formattedValue = FormatEditableAmount(roundedValue);
            var amountChanged = _paidAmount != roundedValue;
            var textChanged = synchronizeText && _paidAmountText != formattedValue;

            if (!amountChanged && !textChanged)
            {
                return;
            }

            _paidAmount = roundedValue;
            if (amountChanged)
            {
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(PaidAmountBase));
                OnPropertyChanged(nameof(RemainingBaseAmount));
                OnPropertyChanged(nameof(RemainingCounterAmount));
            }

            if (textChanged)
            {
                _paidAmountText = formattedValue;
                OnPropertyChanged(nameof(PaidAmountText));
            }
        }

        private static bool TryParseEditableAmount(string? value, out decimal parsedAmount)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                parsedAmount = 0m;
                return true;
            }

            var trimmedValue = value.Trim();
            if (decimal.TryParse(trimmedValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount)
                || decimal.TryParse(trimmedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedAmount))
            {
                return true;
            }

            var normalizedValue = trimmedValue
                .Replace(",", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty);

            var groupSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator;
            if (!string.IsNullOrWhiteSpace(groupSeparator))
            {
                normalizedValue = normalizedValue.Replace(groupSeparator, string.Empty);
            }

            return decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount)
                || decimal.TryParse(normalizedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedAmount);
        }

        private static string FormatEditableAmount(decimal value)
            => value.ToString("#,0.####", CultureInfo.InvariantCulture);

        private static decimal ConvertToBaseAmount(decimal amount, decimal rateToBase)
        {
            var roundedAmount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
            if (roundedAmount <= 0m)
            {
                return 0m;
            }

            var effectiveRate = rateToBase > 0m ? rateToBase : 1m;
            return decimal.Round(roundedAmount * effectiveRate, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal ConvertBaseToCounterAmount(decimal baseAmount, decimal counterRateToBase)
        {
            var roundedBaseAmount = decimal.Round(baseAmount, 4, MidpointRounding.AwayFromZero);
            if (roundedBaseAmount <= 0m)
            {
                return 0m;
            }

            var effectiveRate = counterRateToBase > 0m ? counterRateToBase : 1m;
            return decimal.Round(roundedBaseAmount / effectiveRate, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveCounterRateToBase(UsedCarDto? usedCar)
        {
            if (usedCar == null)
            {
                return 1m;
            }

            if (usedCar.PriceBase > 0m && usedCar.PriceCounter > 0m)
            {
                return decimal.Round(usedCar.PriceBase / usedCar.PriceCounter, 8, MidpointRounding.AwayFromZero);
            }

            if (usedCar.CounterRateToBase > 0m)
            {
                return decimal.Round(usedCar.CounterRateToBase, 8, MidpointRounding.AwayFromZero);
            }

            return 1m;
        }

        private async Task LoadSelectedPurchaseDetailAsync(int? purchaseId)
        {
            var version = ++_selectedPurchaseDetailVersion;
            if (purchaseId is not > 0)
            {
                SelectedPurchaseDetail = null;
                return;
            }

            try
            {
                var detail = await _purchasesApi.GetUsedCarPurchaseAsync(purchaseId.Value);
                if (version != _selectedPurchaseDetailVersion)
                {
                    return;
                }

                SelectedPurchaseDetail = detail;
            }
            catch (Exception ex)
            {
                if (version != _selectedPurchaseDetailVersion)
                {
                    return;
                }

                SelectedPurchaseDetail = null;
                SetStatus($"Loading saved purchase lines failed: {ex.Message}", false);
            }
        }

        private void HandleBackgroundException(Exception ex)
            => SetStatus(ex.Message, false);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

}
