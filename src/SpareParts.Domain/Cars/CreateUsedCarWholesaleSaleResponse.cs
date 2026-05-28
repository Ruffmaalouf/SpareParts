namespace SpareParts.Domain.Cars
{
    public sealed class CreateUsedCarWholesaleSaleResponse
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public int UsedCarId { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
    }
}
