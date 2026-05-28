using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
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
    public sealed class UsedCarWholesaleViewModel : INotifyPropertyChanged
    {
        private readonly ICrudApiClient _crudApi;
        private int? _selectedUsedCarId;
        private int? _selectedCustomerId;
        private DateTime _saleDate = DateTime.Today;
        private string _currencyCode = "USD";
        private decimal _salePrice;
        private string _salePriceText = "0";
        private decimal _paidAmount;
        private string _paidAmountText = "0";
        private string _buyerName = string.Empty;
        private string _buyerPhone = string.Empty;
        private string _paymentMethod = string.Empty;
        private string _notes = string.Empty;
        private bool _isForParts;
        private string _repairDescription = string.Empty;
        private decimal _repairPrice;
        private string _repairPriceText = "0";
        private bool _soldAsIsAcknowledged;
        private bool _isLoading;
        private string _status = "Used-car wholesale is ready.";
        private Brush _statusBrush = Brushes.LightGreen;

        public UsedCarWholesaleViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => _ = LoadAsync());
            SubmitCommand = new RelayCommand(_ => _ = SubmitAsync());
            ResetCommand = new RelayCommand(_ => ResetForm());
            AddRepairItemCommand = new RelayCommand(_ => AddRepairItem());
            RemoveRepairItemCommand = new RelayCommand(item => RemoveRepairItem(item as UsedCarWholesaleRepairItemViewModel));
            ClearRepairItemsCommand = new RelayCommand(_ => ClearRepairItems());
        }

        public ObservableCollection<UsedCarDto> AvailableCars { get; } = new();
        public ObservableCollection<CustomerDto> Customers { get; } = new();
        public ObservableCollection<string> CurrencyOptions { get; } = new();
        public ObservableCollection<UsedCarWholesaleSaleDto> RecentSales { get; } = new();
        public ObservableCollection<UsedCarWholesaleRepairItemViewModel> RepairItems { get; } = new();

        public int? SelectedUsedCarId
        {
            get => _selectedUsedCarId;
            set
            {
                if (_selectedUsedCarId == value) return;
                _selectedUsedCarId = value;
                OnPropertyChanged(nameof(SelectedUsedCarId));
                OnPropertyChanged(nameof(SelectedUsedCar));
                OnPropertyChanged(nameof(SelectedCarSummary));
                OnPropertyChanged(nameof(LoadedCostLabel));
                OnPropertyChanged(nameof(ProjectedProfitLoss));
                OnPropertyChanged(nameof(ProjectedProfitLossLabel));
                OnPropertyChanged(nameof(ScreenSummary));

                if (SelectedUsedCar is { } selectedCar)
                {
                    CurrencyCode = NormalizeCurrencyCode(selectedCar.BaseCurrencyCode) ?? CurrencyCode;
                }

                if (SelectedUsedCar is { WholesaleSalePrice: > 0 } soldCar)
                {
                    SalePrice = soldCar.WholesaleSalePrice;
                }
            }
        }

        public int? SelectedCustomerId
        {
            get => _selectedCustomerId;
            set
            {
                if (_selectedCustomerId == value) return;
                _selectedCustomerId = value;
                OnPropertyChanged(nameof(SelectedCustomerId));
                ApplySelectedCustomer();
            }
        }

        public DateTime SaleDate
        {
            get => _saleDate;
            set
            {
                if (_saleDate == value) return;
                _saleDate = value;
                OnPropertyChanged(nameof(SaleDate));
            }
        }

        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                var normalized = NormalizeCurrencyCode(value) ?? "USD";
                if (_currencyCode == normalized) return;
                _currencyCode = normalized;
                OnPropertyChanged(nameof(CurrencyCode));
                OnPropertyChanged(nameof(RepairTotalLabel));
            }
        }

        public string SalePriceText
        {
            get => _salePriceText;
            set
            {
                if (_salePriceText == value) return;
                _salePriceText = value;
                OnPropertyChanged(nameof(SalePriceText));

                if (TryParseEditableAmount(value, out var parsedAmount))
                {
                    SetSalePrice(parsedAmount, synchronizeText: false);
                }
            }
        }

        public decimal SalePrice
        {
            get => _salePrice;
            set => SetSalePrice(value, synchronizeText: true);
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

        public string BuyerName
        {
            get => _buyerName;
            set
            {
                if (_buyerName == value) return;
                _buyerName = value;
                OnPropertyChanged(nameof(BuyerName));
            }
        }

        public string BuyerPhone
        {
            get => _buyerPhone;
            set
            {
                if (_buyerPhone == value) return;
                _buyerPhone = value;
                OnPropertyChanged(nameof(BuyerPhone));
            }
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (_paymentMethod == value) return;
                _paymentMethod = value;
                OnPropertyChanged(nameof(PaymentMethod));
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

        public bool IsForParts
        {
            get => _isForParts;
            set
            {
                if (_isForParts == value) return;
                _isForParts = value;
                OnPropertyChanged(nameof(IsForParts));
                OnPropertyChanged(nameof(IsRepairListEnabled));
                OnPropertyChanged(nameof(SaleConditionLabel));
                OnPropertyChanged(nameof(ProjectedProfitLoss));
                OnPropertyChanged(nameof(ProjectedProfitLossLabel));
                OnPropertyChanged(nameof(RepairTotalLabel));
            }
        }

        public string RepairDescription
        {
            get => _repairDescription;
            set
            {
                if (_repairDescription == value) return;
                _repairDescription = value;
                OnPropertyChanged(nameof(RepairDescription));
            }
        }

        public string RepairPriceText
        {
            get => _repairPriceText;
            set
            {
                if (_repairPriceText == value) return;
                _repairPriceText = value;
                OnPropertyChanged(nameof(RepairPriceText));

                if (TryParseEditableAmount(value, out var parsedAmount))
                {
                    SetRepairPrice(parsedAmount, synchronizeText: false);
                }
            }
        }

        public decimal RepairPrice
        {
            get => _repairPrice;
            set => SetRepairPrice(value, synchronizeText: true);
        }

        public bool SoldAsIsAcknowledged
        {
            get => _soldAsIsAcknowledged;
            set
            {
                if (_soldAsIsAcknowledged == value) return;
                _soldAsIsAcknowledged = value;
                OnPropertyChanged(nameof(SoldAsIsAcknowledged));
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
            ? AvailableCars.FirstOrDefault(item => item.Id == usedCarId)
            : null;

        public string SelectedCarSummary => SelectedUsedCar == null
            ? "Select an unsold used car to prepare an as-is wholesale sale."
            : $"{SelectedUsedCar.Car} {SelectedUsedCar.ModelYear} - loaded cost {SelectedUsedCar.FullCostBase:N2} {SelectedUsedCar.BaseCurrencyCode}.";

        public string LoadedCostLabel => SelectedUsedCar == null
            ? "-"
            : $"{SelectedUsedCar.FullCostBase:N2} {SelectedUsedCar.BaseCurrencyCode}";

        public decimal ProjectedProfitLoss => SelectedUsedCar == null
            ? 0m
            : decimal.Round(SalePrice - SelectedUsedCar.FullCostBase - (IsForParts ? 0m : RepairTotalAmount), 2, MidpointRounding.AwayFromZero);

        public string ProjectedProfitLossLabel => $"Projected result: {ProjectedProfitLoss:N2} {SelectedUsedCar?.BaseCurrencyCode ?? CurrencyCode}";
        public bool IsRepairListEnabled => !IsForParts;
        public string SaleConditionLabel => IsForParts ? "For parts only" : "Repair items reduce profit";
        public decimal RepairTotalAmount => decimal.Round(RepairItems.Sum(item => item.Price), 4, MidpointRounding.AwayFromZero);
        public string RepairTotalLabel => $"Repair total: {RepairTotalAmount:N2} {CurrencyCode}";

        public string ScreenSummary
        {
            get
            {
                var unsoldCount = AvailableCars.Count;
                return $"{unsoldCount} available car(s), {RecentSales.Count} wholesale sale(s) recorded.";
            }
        }

        public ICommand LoadCommand { get; }
        public ICommand SubmitCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand AddRepairItemCommand { get; }
        public ICommand RemoveRepairItemCommand { get; }
        public ICommand ClearRepairItemsCommand { get; }

        public async Task LoadAsync()
        {
            IsLoading = true;
            SetStatus("Loading used-car wholesale workspace...", true);

            try
            {
                var selectedUsedCarId = SelectedUsedCarId;
                var selectedCustomerId = SelectedCustomerId;
                var usedCarsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");
                var customersTask = _crudApi.GetAllAsync<CustomerDto>("api/customers");
                var salesTask = _crudApi.GetAllAsync<UsedCarWholesaleSaleDto>("api/usedcars/wholesale-sales");
                var constantsTask = _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");

                await Task.WhenAll(usedCarsTask, customersTask, salesTask, constantsTask);

                Replace(AvailableCars, usedCarsTask.Result
                    .Where(item => !item.IsWholesaleSold)
                    .OrderBy(item => item.Car)
                    .ThenByDescending(item => item.ModelYear));
                Replace(Customers, customersTask.Result.OrderBy(item => item.Name));
                Replace(RecentSales, salesTask.Result.OrderByDescending(item => item.SaleDate));
                ApplyCurrencyOptions(constantsTask.Result);

                SelectedUsedCarId = AvailableCars.Any(item => item.Id == selectedUsedCarId)
                    ? selectedUsedCarId
                    : AvailableCars.FirstOrDefault()?.Id;
                SelectedCustomerId = Customers.Any(item => item.Id == selectedCustomerId)
                    ? selectedCustomerId
                    : null;

                OnPropertyChanged(nameof(ScreenSummary));
                SetStatus("Used-car wholesale workspace loaded.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Loading used-car wholesale failed: {ex.Message}", false);
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

            if (usedCar.IsWholesaleSold)
            {
                SetStatus("This used car already has a wholesale sale recorded.", false);
                return;
            }

            if (SalePrice <= 0)
            {
                SetStatus("Enter a sale price greater than zero.", false);
                return;
            }

            if (PaidAmount < 0)
            {
                SetStatus("Paid amount cannot be negative.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(BuyerName) && SelectedCustomerId is not > 0)
            {
                SetStatus("Choose a customer or type the wholesale buyer name.", false);
                return;
            }

            if (!SoldAsIsAcknowledged)
            {
                SetStatus("Confirm the as-is acknowledgement before recording the sale.", false);
                return;
            }

            if (!IsForParts && RepairItems.Any(item => item.Price < 0m))
            {
                SetStatus("Repair item prices cannot be negative.", false);
                return;
            }

            try
            {
                var request = new CreateUsedCarWholesaleSaleRequest
                {
                    CustomerId = SelectedCustomerId,
                    BuyerName = BuyerName,
                    BuyerPhone = BuyerPhone,
                    SaleDate = SaleDate,
                    CurrencyCode = CurrencyCode,
                    SalePrice = SalePrice,
                    PaidAmount = PaidAmount,
                    IsForParts = IsForParts,
                    RepairItems = IsForParts
                        ? new List<UsedCarWholesaleRepairItemDto>()
                        : RepairItems.Select(item => new UsedCarWholesaleRepairItemDto
                        {
                            Description = item.Description,
                            Price = item.Price
                        }).ToList(),
                    PaymentMethod = PaymentMethod,
                    Notes = Notes,
                    SoldAsIsAcknowledged = SoldAsIsAcknowledged
                };

                var result = await _crudApi.PostAsync<CreateUsedCarWholesaleSaleResponse>(
                    $"api/usedcars/{usedCar.Id}/wholesale-sales",
                    request);

                AppNotificationCenter.Instance.Publish($"Used-car wholesale sale {result.SaleNumber} recorded.", true);
                SetStatus($"Wholesale sale {result.SaleNumber} recorded as {result.PaymentStatus}.", true);
                ResetForm(clearSelectedCar: true);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                AppNotificationCenter.Instance.Publish(ex.Message, false);
                SetStatus($"Recording used-car wholesale sale failed: {ex.Message}", false);
            }
        }

        private void ResetForm(bool clearSelectedCar = false)
        {
            if (clearSelectedCar)
            {
                SelectedUsedCarId = null;
            }

            SelectedCustomerId = null;
            SaleDate = DateTime.Today;
            SalePrice = 0m;
            PaidAmount = 0m;
            BuyerName = string.Empty;
            BuyerPhone = string.Empty;
            PaymentMethod = string.Empty;
            Notes = string.Empty;
            IsForParts = false;
            RepairDescription = string.Empty;
            RepairPrice = 0m;
            ClearRepairItems();
            SoldAsIsAcknowledged = false;
            SetStatus("Used-car wholesale form reset.", true);
        }

        private void ApplySelectedCustomer()
        {
            var customer = SelectedCustomerId is int customerId
                ? Customers.FirstOrDefault(item => item.Id == customerId)
                : null;
            if (customer == null)
            {
                return;
            }

            BuyerName = customer.Name;
            BuyerPhone = customer.Phone ?? string.Empty;
        }

        private void ApplyCurrencyOptions(IEnumerable<AppConstantDto> constants)
        {
            var options = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "USD" };
            foreach (var constant in constants)
            {
                if (constant.Key.Contains("CurrencyCode", StringComparison.OrdinalIgnoreCase)
                    && NormalizeCurrencyCode(constant.Value) is { } currencyCode)
                {
                    options.Add(currencyCode);
                }
            }

            Replace(CurrencyOptions, options.Select(item => item.ToUpperInvariant()));
            if (!CurrencyOptions.Contains(CurrencyCode))
            {
                CurrencyCode = CurrencyOptions.FirstOrDefault() ?? "USD";
            }
        }

        private void SetSalePrice(decimal value, bool synchronizeText)
        {
            var roundedValue = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
            var formattedValue = FormatEditableAmount(roundedValue);
            var amountChanged = _salePrice != roundedValue;
            var textChanged = synchronizeText && _salePriceText != formattedValue;

            if (!amountChanged && !textChanged)
            {
                return;
            }

            _salePrice = roundedValue;
            if (amountChanged)
            {
                OnPropertyChanged(nameof(SalePrice));
                OnPropertyChanged(nameof(ProjectedProfitLoss));
                OnPropertyChanged(nameof(ProjectedProfitLossLabel));
            }

            if (textChanged)
            {
                _salePriceText = formattedValue;
                OnPropertyChanged(nameof(SalePriceText));
            }
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
            }

            if (textChanged)
            {
                _paidAmountText = formattedValue;
                OnPropertyChanged(nameof(PaidAmountText));
            }
        }

        private void AddRepairItem()
        {
            if (IsForParts)
            {
                SetStatus("Repair items are not needed when the car is sold for parts.", false);
                return;
            }

            var description = RepairDescription?.Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                SetStatus("Enter a repair item description.", false);
                return;
            }

            if (RepairPrice < 0m)
            {
                SetStatus("Repair item price cannot be negative.", false);
                return;
            }

            RepairItems.Add(new UsedCarWholesaleRepairItemViewModel
            {
                Description = description,
                Price = decimal.Round(RepairPrice, 4, MidpointRounding.AwayFromZero)
            });
            RepairDescription = string.Empty;
            RepairPrice = 0m;
            RaiseRepairTotals();
        }

        private void RemoveRepairItem(UsedCarWholesaleRepairItemViewModel? item)
        {
            if (item == null)
            {
                return;
            }

            RepairItems.Remove(item);
            RaiseRepairTotals();
        }

        private void ClearRepairItems()
        {
            if (RepairItems.Count == 0)
            {
                RaiseRepairTotals();
                return;
            }

            RepairItems.Clear();
            RaiseRepairTotals();
        }

        private void SetRepairPrice(decimal value, bool synchronizeText)
        {
            var roundedValue = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
            var formattedValue = FormatEditableAmount(roundedValue);
            var amountChanged = _repairPrice != roundedValue;
            var textChanged = synchronizeText && _repairPriceText != formattedValue;

            if (!amountChanged && !textChanged)
            {
                return;
            }

            _repairPrice = roundedValue;
            if (amountChanged)
            {
                OnPropertyChanged(nameof(RepairPrice));
            }

            if (textChanged)
            {
                _repairPriceText = formattedValue;
                OnPropertyChanged(nameof(RepairPriceText));
            }
        }

        private void RaiseRepairTotals()
        {
            OnPropertyChanged(nameof(RepairTotalAmount));
            OnPropertyChanged(nameof(RepairTotalLabel));
            OnPropertyChanged(nameof(ProjectedProfitLoss));
            OnPropertyChanged(nameof(ProjectedProfitLossLabel));
        }

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

    public sealed class UsedCarWholesaleRepairItemViewModel
    {
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
