namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarDto
    {
        public int Id { get; set; }
        public string? Barcode { get; set; }
        public int? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int CarModelId { get; set; }
        public string Car { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string PriceCurrency { get; set; } = "USD";
        public decimal Price { get; set; }
        public decimal PriceBase { get; set; }
        public decimal PriceCounter { get; set; }
        public int? LocationId { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Transportation { get; set; }
        public bool IsReceived { get; set; }
        public bool IsShipped { get; set; }
        public decimal PartOut { get; set; }
        public decimal Shipping { get; set; }
        public decimal Customs { get; set; }
        public decimal Repairs { get; set; }
        public decimal TotalBeforeShipping { get; set; }
        public decimal GrandTotalBase { get; set; }
        public decimal GrandTotalCounter { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
        public decimal PurchaseCostBase { get; set; }
        public decimal TransportationCostBase { get; set; }
        public decimal CustomsCostBase { get; set; }
        public decimal ShippingCostBase { get; set; }
        public decimal PartOutCostBase { get; set; }
        public decimal RepairsCostBase { get; set; }
        public decimal FullCostBase { get; set; }
        public int PartsRemovedCount { get; set; }
        public decimal PartsRemovedValueBase { get; set; }
        public decimal PartsSoldQuantity { get; set; }
        public decimal PartsSoldAmountBase { get; set; }
        public decimal SalePriceBase { get; set; }
        public decimal RemainingStockQuantity { get; set; }
        public decimal RemainingStockValueBase { get; set; }
        public decimal NetProfitLossBase { get; set; }
    }
}
