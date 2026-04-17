namespace SpareParts.Domain.Cars
{
    public sealed class CreateUsedCarRequest
    {
        public int CarModelId { get; set; }
        public int ModelYear { get; set; }
        public string PriceCurrency { get; set; } = "USD";
        public decimal Price { get; set; }
        public int LocationId { get; set; }
        public bool IsReceived { get; set; }
        public bool IsShipped { get; set; }
        public decimal PartOut { get; set; }
        public decimal Shipping { get; set; }
        public decimal Customs { get; set; }
    }
}
