using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Purchases;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        private readonly ICrudApiClient _crudApi;
        private readonly IAccountingApiClient _accountingApi;
        private readonly IPurchasesApiClient _purchasesApi;
        private readonly Dictionary<string, int?> _defaultAccountsByRole = new(StringComparer.OrdinalIgnoreCase);

        private int? _selectedUsedCarId;
        private int? _selectedSupplierId;
        private DateTime _purchaseDate = DateTime.Today;
        private decimal _paidAmount;
        private string _notes = string.Empty;
        private bool _isLoading;
        private string _status = "Used-car purchases are ready.";
        private Brush _statusBrush = Brushes.LightGreen;

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
                OnPropertyChanged(nameof(TotalBaseLabel));
                OnPropertyChanged(nameof(PaidAmountLabel));
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

        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (_paidAmount == value) return;
                _paidAmount = value;
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingBaseAmount));
            }
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

        public UsedCarDto? SelectedUsedCar => SelectedUsedCarId is int usedCarId
            ? UsedCars.FirstOrDefault(item => item.Id == usedCarId)
            : null;

        public string BaseCurrencyCode => SelectedUsedCar?.BaseCurrencyCode ?? "USD";
        public string TotalBaseLabel => $"Total ({BaseCurrencyCode})";
        public string PaidAmountLabel => $"Paid ({BaseCurrencyCode})";
        public decimal TotalBaseAmount => decimal.Round(PurchaseLines.Sum(line => line.BaseAmount), 4, MidpointRounding.AwayFromZero);
        public decimal RemainingBaseAmount => decimal.Round(Math.Max(TotalBaseAmount - PaidAmount, 0m), 4, MidpointRounding.AwayFromZero);
        public string ScreenSummary => $"{RecentPurchases.Count} used-car purchase(s) recorded.";

        public ICommand LoadCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand RefreshLinesCommand { get; }

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
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            SetStatus("Loading used-car purchase workspace…", true);

            try
            {
                var selectedUsedCarId = SelectedUsedCarId;
                var selectedSupplierId = SelectedSupplierId;

                var usedCarsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");
                var suppliersTask = _crudApi.GetAllAsync<SupplierDto>("api/suppliers");
                var accountsTask = _accountingApi.GetAccountsAsync();
                var postingSettingsTask = _accountingApi.GetPostingSettingsAsync();
                var purchasesTask = _purchasesApi.GetUsedCarPurchasesAsync();

                await Task.WhenAll(usedCarsTask, suppliersTask, accountsTask, postingSettingsTask, purchasesTask);

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

                SelectedUsedCarId = UsedCars.Any(item => item.Id == selectedUsedCarId)
                    ? selectedUsedCarId
                    : UsedCars.FirstOrDefault()?.Id;

                SelectedSupplierId = Suppliers.Any(item => item.Id == selectedSupplierId)
                    ? selectedSupplierId
                    : SelectedSupplierId ?? Suppliers.FirstOrDefault()?.Id;

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
                SetStatus("The selected used car has no positive amount lines to post.", false);
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
                    PaidAmount = PaidAmount,
                    BaseCurrencyCode = BaseCurrencyCode,
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
                            AccountId = line.AccountId ?? 0,
                            SortOrder = line.SortOrder
                        })
                        .ToList()
                };

                var result = await _purchasesApi.CreateUsedCarPurchaseAsync(request);
                AppNotificationCenter.Instance.Publish($"✓ Used-car purchase {result.PurchaseNumber} created.", true);
                SetStatus($"Used-car purchase {result.PurchaseNumber} created.", true);

                PaidAmount = 0m;
                Notes = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
                SetStatus($"Posting used-car purchase failed: {ex.Message}", false);
            }
        }

        private void ResetForm()
        {
            PurchaseDate = DateTime.Today;
            PaidAmount = 0m;
            Notes = string.Empty;
            GeneratePurchaseLines();
            SetStatus("Used-car purchase form reset.", true);
        }

        private void GeneratePurchaseLines()
        {
            PurchaseLines.Clear();

            var usedCar = SelectedUsedCar;
            if (usedCar == null)
            {
                OnPropertyChanged(nameof(TotalBaseAmount));
                OnPropertyChanged(nameof(RemainingBaseAmount));
                return;
            }

            var counterRate = usedCar.CounterRateToBase > 0m ? usedCar.CounterRateToBase : 1m;
            var priceRate = usedCar.Price > 0m && usedCar.PriceBase > 0m
                ? decimal.Round(usedCar.PriceBase / usedCar.Price, 8, MidpointRounding.AwayFromZero)
                : 1m;

            AddLine(UsedCarPriceRoleKey, "Vehicle Price", usedCar.Price, usedCar.PriceCurrency, priceRate, usedCar.PriceBase);
            AddLine(UsedCarTransportationRoleKey, "Transportation", usedCar.Transportation, usedCar.CounterCurrencyCode, counterRate);
            AddLine(UsedCarPartOutRoleKey, "Part-Out", usedCar.PartOut, usedCar.CounterCurrencyCode, counterRate);
            AddLine(UsedCarShippingRoleKey, "Shipping", usedCar.Shipping, usedCar.CounterCurrencyCode, counterRate);
            AddLine(UsedCarCustomsRoleKey, "Customs", usedCar.Customs, usedCar.CounterCurrencyCode, counterRate);

            OnPropertyChanged(nameof(TotalBaseAmount));
            OnPropertyChanged(nameof(RemainingBaseAmount));
        }

        private void AddLine(string roleKey, string description, decimal amount, string currencyCode, decimal rateToBase, decimal? baseAmountOverride = null)
        {
            var roundedAmount = decimal.Round(amount, 4, MidpointRounding.AwayFromZero);
            if (roundedAmount <= 0m)
            {
                return;
            }

            var effectiveRate = rateToBase > 0m ? rateToBase : 1m;
            var baseAmount = baseAmountOverride is > 0m
                ? decimal.Round(baseAmountOverride.Value, 4, MidpointRounding.AwayFromZero)
                : decimal.Round(roundedAmount * effectiveRate, 4, MidpointRounding.AwayFromZero);

            PurchaseLines.Add(new UsedCarPurchaseLineViewModel
            {
                SortOrder = PurchaseLines.Count + 1,
                RoleKey = roleKey,
                Description = description,
                Amount = roundedAmount,
                CurrencyCode = NormalizeCurrencyCode(currencyCode) ?? BaseCurrencyCode,
                RateToBase = decimal.Round(effectiveRate, 8, MidpointRounding.AwayFromZero),
                BaseAmount = baseAmount,
                AccountId = ResolveDefaultAccountId(roleKey)
            });
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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class PurchaseAccountOption
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    public sealed class UsedCarPurchaseLineViewModel : INotifyPropertyChanged
    {
        private int? _accountId;

        public int SortOrder { get; set; }
        public string RoleKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal RateToBase { get; set; }
        public decimal BaseAmount { get; set; }

        public int? AccountId
        {
            get => _accountId;
            set
            {
                if (_accountId == value) return;
                _accountId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AccountId)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
