using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpareParts.Desktop.Wpf.Management
{
    public class UsedCarEntry : INotifyPropertyChanged
    {
        private int _id;
        private string? _barcode;
        private int? _supplierId;
        private int? _carModelId;
        private int? _locationId;
        private string _supplierName = string.Empty;
        private string _car = string.Empty;
        private int _modelYear;
        private string _priceCurrency = "USD";
        private decimal _price;
        private decimal _priceBase;
        private decimal _priceCounter;
        private string _location = string.Empty;
        private decimal _transportation;
        private bool _isReceived;
        private bool _isShipped;
        private decimal _partOut;
        private decimal _shipping;
        private decimal _customs;
        private decimal _repairs;
        private decimal _totalBeforeShipping;
        private decimal _grandTotalBase;
        private decimal _grandTotalCounter;
        private decimal _purchaseCostBase;
        private decimal _transportationCostBase;
        private decimal _customsCostBase;
        private decimal _shippingCostBase;
        private decimal _partOutCostBase;
        private decimal _repairsCostBase;
        private decimal _fullCostBase;
        private int _partsRemovedCount;
        private decimal _partsRemovedValueBase;
        private decimal _partsSoldQuantity;
        private decimal _partsSoldAmountBase;
        private decimal _salePriceBase;
        private bool _isWholesaleSold;
        private bool _isWholesaleForParts;
        private string? _wholesaleSaleNumber;
        private string _wholesalePaymentStatus = string.Empty;
        private decimal _remainingStockQuantity;
        private decimal _remainingStockValueBase;
        private decimal _netProfitLossBase;
        private string _displayCurrencyCode = "USD";
        private decimal _displayRateToBase = 1m;

        private static readonly HashSet<string> ProfitProjectSourceProperties = new(StringComparer.Ordinal)
        {
            nameof(PriceBase),
            nameof(GrandTotalBase),
            nameof(PurchaseCostBase),
            nameof(TransportationCostBase),
            nameof(CustomsCostBase),
            nameof(ShippingCostBase),
            nameof(PartOutCostBase),
            nameof(RepairsCostBase),
            nameof(FullCostBase),
            nameof(PartsRemovedCount),
            nameof(PartsRemovedValueBase),
            nameof(PartsSoldQuantity),
            nameof(PartsSoldAmountBase),
            nameof(SalePriceBase),
            nameof(RemainingStockQuantity),
            nameof(RemainingStockValueBase),
            nameof(NetProfitLossBase),
            nameof(DisplayCurrencyCode),
            nameof(DisplayRateToBase)
        };

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string? Barcode
        {
            get => _barcode;
            set => SetField(ref _barcode, value);
        }

        public int? SupplierId
        {
            get => _supplierId;
            set => SetField(ref _supplierId, value);
        }

        public string SupplierName
        {
            get => _supplierName;
            set => SetField(ref _supplierName, value);
        }

        public int? CarModelId
        {
            get => _carModelId;
            set => SetField(ref _carModelId, value);
        }

        public string Car
        {
            get => _car;
            set => SetField(ref _car, value);
        }

        public int? LocationId
        {
            get => _locationId;
            set => SetField(ref _locationId, value);
        }

        public int ModelYear
        {
            get => _modelYear;
            set => SetField(ref _modelYear, value);
        }

        public string PriceCurrency
        {
            get => _priceCurrency;
            set => SetField(ref _priceCurrency, value);
        }

        public decimal Price
        {
            get => _price;
            set => SetField(ref _price, value);
        }

        public decimal PriceBase
        {
            get => _priceBase;
            set => SetField(ref _priceBase, value);
        }

        public decimal PriceCounter
        {
            get => _priceCounter;
            set => SetField(ref _priceCounter, value);
        }

        public string Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        public decimal Transportation
        {
            get => _transportation;
            set => SetField(ref _transportation, value);
        }

        public bool IsReceived
        {
            get => _isReceived;
            set => SetField(ref _isReceived, value);
        }

        public bool IsShipped
        {
            get => _isShipped;
            set => SetField(ref _isShipped, value);
        }

        public decimal TotalBeforeShipping
        {
            get => _totalBeforeShipping;
            set => SetField(ref _totalBeforeShipping, value);
        }

        public decimal PartOut
        {
            get => _partOut;
            set => SetField(ref _partOut, value);
        }

        public decimal Shipping
        {
            get => _shipping;
            set => SetField(ref _shipping, value);
        }

        public decimal Customs
        {
            get => _customs;
            set => SetField(ref _customs, value);
        }

        public decimal Repairs
        {
            get => _repairs;
            set => SetField(ref _repairs, value);
        }

        public decimal GrandTotalBase
        {
            get => _grandTotalBase;
            set => SetField(ref _grandTotalBase, value);
        }

        public decimal GrandTotalCounter
        {
            get => _grandTotalCounter;
            set => SetField(ref _grandTotalCounter, value);
        }

        public decimal PurchaseCostBase
        {
            get => _purchaseCostBase;
            set => SetField(ref _purchaseCostBase, value);
        }

        public decimal TransportationCostBase
        {
            get => _transportationCostBase;
            set => SetField(ref _transportationCostBase, value);
        }

        public decimal CustomsCostBase
        {
            get => _customsCostBase;
            set => SetField(ref _customsCostBase, value);
        }

        public decimal ShippingCostBase
        {
            get => _shippingCostBase;
            set => SetField(ref _shippingCostBase, value);
        }

        public decimal PartOutCostBase
        {
            get => _partOutCostBase;
            set => SetField(ref _partOutCostBase, value);
        }

        public decimal RepairsCostBase
        {
            get => _repairsCostBase;
            set => SetField(ref _repairsCostBase, value);
        }

        public decimal FullCostBase
        {
            get => _fullCostBase;
            set => SetField(ref _fullCostBase, value);
        }

        public int PartsRemovedCount
        {
            get => _partsRemovedCount;
            set => SetField(ref _partsRemovedCount, value);
        }

        public decimal PartsRemovedValueBase
        {
            get => _partsRemovedValueBase;
            set => SetField(ref _partsRemovedValueBase, value);
        }

        public decimal PartsSoldQuantity
        {
            get => _partsSoldQuantity;
            set => SetField(ref _partsSoldQuantity, value);
        }

        public decimal PartsSoldAmountBase
        {
            get => _partsSoldAmountBase;
            set => SetField(ref _partsSoldAmountBase, value);
        }

        public decimal SalePriceBase
        {
            get => _salePriceBase;
            set => SetField(ref _salePriceBase, value);
        }

        public bool IsWholesaleSold
        {
            get => _isWholesaleSold;
            set
            {
                if (!SetField(ref _isWholesaleSold, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(SaleStatusText));
            }
        }

        public bool IsWholesaleForParts
        {
            get => _isWholesaleForParts;
            set
            {
                if (!SetField(ref _isWholesaleForParts, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(SaleStatusText));
            }
        }

        public string? WholesaleSaleNumber
        {
            get => _wholesaleSaleNumber;
            set
            {
                if (!SetField(ref _wholesaleSaleNumber, value))
                {
                    return;
                }

                OnPropertyChanged(nameof(SaleStatusText));
            }
        }

        public string WholesalePaymentStatus
        {
            get => _wholesalePaymentStatus;
            set => SetField(ref _wholesalePaymentStatus, value ?? string.Empty);
        }

        public string SaleStatusText
        {
            get
            {
                if (!IsWholesaleSold)
                {
                    return "Available";
                }

                var mode = IsWholesaleForParts ? "Sold for parts" : "Sold wholesale";
                return string.IsNullOrWhiteSpace(WholesaleSaleNumber)
                    ? mode
                    : $"{mode} - {WholesaleSaleNumber}";
            }
        }

        public decimal RemainingStockQuantity
        {
            get => _remainingStockQuantity;
            set => SetField(ref _remainingStockQuantity, value);
        }

        public decimal RemainingStockValueBase
        {
            get => _remainingStockValueBase;
            set => SetField(ref _remainingStockValueBase, value);
        }

        public decimal NetProfitLossBase
        {
            get => _netProfitLossBase;
            set => SetField(ref _netProfitLossBase, value);
        }

        public string DisplayCurrencyCode
        {
            get => _displayCurrencyCode;
            set => SetField(ref _displayCurrencyCode, NormalizeDisplayCurrencyCode(value));
        }

        public decimal DisplayRateToBase
        {
            get => _displayRateToBase;
            set => SetField(ref _displayRateToBase, value > 0m ? decimal.Round(value, 8, MidpointRounding.AwayFromZero) : 1m);
        }

        public decimal TeardownCostBase => RoundMoney(TransportationCostBase + CustomsCostBase + ShippingCostBase + PartOutCostBase + RepairsCostBase);

        public decimal ProfitMapSoldValueBase => RoundMoney(PartsSoldAmountBase != 0m ? PartsSoldAmountBase : SalePriceBase);

        public decimal ProfitMapRecoveredValueBase => RoundMoney(ProfitMapSoldValueBase + RemainingStockValueBase);

        public decimal ProfitMapBreakEvenGapBase => RoundMoney(FullCostBase > ProfitMapRecoveredValueBase ? FullCostBase - ProfitMapRecoveredValueBase : 0m);

        public decimal PriceDisplay => ConvertBaseToDisplay(PriceBase);

        public decimal GrandTotalDisplay => ConvertBaseToDisplay(GrandTotalBase);

        public decimal PurchaseCostDisplay => ConvertBaseToDisplay(PurchaseCostBase);

        public decimal TeardownCostDisplay => ConvertBaseToDisplay(TeardownCostBase);

        public decimal PartsRemovedValueDisplay => ConvertBaseToDisplay(PartsRemovedValueBase);

        public decimal ProfitMapSoldValueDisplay => ConvertBaseToDisplay(ProfitMapSoldValueBase);

        public decimal RemainingStockValueDisplay => ConvertBaseToDisplay(RemainingStockValueBase);

        public decimal NetProfitLossDisplay => ConvertBaseToDisplay(NetProfitLossBase);

        public decimal FullCostDisplay => ConvertBaseToDisplay(FullCostBase);

        public decimal ProfitMapRecoveredValueDisplay => ConvertBaseToDisplay(ProfitMapRecoveredValueBase);

        public decimal ProfitMapBreakEvenGapDisplay => ConvertBaseToDisplay(ProfitMapBreakEvenGapBase);

        public string PriceDisplayText => FormatDisplay(PriceDisplay);

        public string GrandTotalDisplayText => FormatDisplay(GrandTotalDisplay);

        public string PurchaseCostDisplayText => FormatDisplay(PurchaseCostDisplay);

        public string TeardownCostDisplayText => FormatDisplay(TeardownCostDisplay);

        public string PartsRemovedValueDisplayText => FormatDisplay(PartsRemovedValueDisplay);

        public string ProfitMapSoldValueDisplayText => FormatDisplay(ProfitMapSoldValueDisplay);

        public string RemainingStockValueDisplayText => FormatDisplay(RemainingStockValueDisplay);

        public string NetProfitLossDisplayText => FormatDisplay(NetProfitLossDisplay);

        public string FullCostDisplayText => FormatDisplay(FullCostDisplay);

        public string ProfitMapRecoveredValueDisplayText => FormatDisplay(ProfitMapRecoveredValueDisplay);

        public string ProfitMapBreakEvenGapDisplayText => FormatDisplay(ProfitMapBreakEvenGapDisplay);

        public decimal ProfitMapRecoveredPercent => FullCostBase <= 0m
            ? 0m
            : decimal.Round(ProfitMapRecoveredValueBase / FullCostBase * 100m, 0, MidpointRounding.AwayFromZero);

        public double ProfitMapRecoveredPercentCapped => (double)(ProfitMapRecoveredPercent > 100m ? 100m : ProfitMapRecoveredPercent);

        public string ProfitMapRecoveredPercentText => $"{ProfitMapRecoveredPercent:N0}% recovered";

        public string ProfitMapBreakEvenStatus => ProfitMapBreakEvenGapBase > 0m
            ? $"{DisplayCurrencyCode} {ProfitMapBreakEvenGapDisplay:N2} to break even"
            : "Break-even reached";

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            if (propertyName == null || ProfitProjectSourceProperties.Contains(propertyName))
            {
                RaiseProfitProjectProperties();
            }

            return true;
        }

        private void RaiseProfitProjectProperties()
        {
            OnPropertyChanged(nameof(TeardownCostBase));
            OnPropertyChanged(nameof(ProfitMapSoldValueBase));
            OnPropertyChanged(nameof(ProfitMapRecoveredValueBase));
            OnPropertyChanged(nameof(ProfitMapBreakEvenGapBase));
            OnPropertyChanged(nameof(PriceDisplay));
            OnPropertyChanged(nameof(GrandTotalDisplay));
            OnPropertyChanged(nameof(PurchaseCostDisplay));
            OnPropertyChanged(nameof(TeardownCostDisplay));
            OnPropertyChanged(nameof(PartsRemovedValueDisplay));
            OnPropertyChanged(nameof(ProfitMapSoldValueDisplay));
            OnPropertyChanged(nameof(RemainingStockValueDisplay));
            OnPropertyChanged(nameof(NetProfitLossDisplay));
            OnPropertyChanged(nameof(FullCostDisplay));
            OnPropertyChanged(nameof(ProfitMapRecoveredValueDisplay));
            OnPropertyChanged(nameof(ProfitMapBreakEvenGapDisplay));
            OnPropertyChanged(nameof(PriceDisplayText));
            OnPropertyChanged(nameof(GrandTotalDisplayText));
            OnPropertyChanged(nameof(PurchaseCostDisplayText));
            OnPropertyChanged(nameof(TeardownCostDisplayText));
            OnPropertyChanged(nameof(PartsRemovedValueDisplayText));
            OnPropertyChanged(nameof(ProfitMapSoldValueDisplayText));
            OnPropertyChanged(nameof(RemainingStockValueDisplayText));
            OnPropertyChanged(nameof(NetProfitLossDisplayText));
            OnPropertyChanged(nameof(FullCostDisplayText));
            OnPropertyChanged(nameof(ProfitMapRecoveredValueDisplayText));
            OnPropertyChanged(nameof(ProfitMapBreakEvenGapDisplayText));
            OnPropertyChanged(nameof(ProfitMapRecoveredPercent));
            OnPropertyChanged(nameof(ProfitMapRecoveredPercentCapped));
            OnPropertyChanged(nameof(ProfitMapRecoveredPercentText));
            OnPropertyChanged(nameof(ProfitMapBreakEvenStatus));
        }

        private decimal ConvertBaseToDisplay(decimal baseAmount)
        {
            var rate = DisplayRateToBase > 0m ? DisplayRateToBase : 1m;
            return RoundMoney(baseAmount / rate);
        }

        private string FormatDisplay(decimal amount) => $"{DisplayCurrencyCode} {amount:N2}";

        private static decimal RoundMoney(decimal amount)
            => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

        private static string NormalizeDisplayCurrencyCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "USD";
            }

            var normalized = value.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : "USD";
        }

        private void OnPropertyChanged(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
