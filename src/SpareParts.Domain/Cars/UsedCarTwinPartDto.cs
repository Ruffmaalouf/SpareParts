namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarTwinPartDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string InternalCode { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string Condition { get; set; } = string.Empty;
        public DateTime ListedAt { get; set; }
        public decimal RemainingQuantity { get; set; }
        public bool IsSold { get; set; }
        public DateTime? SoldAt { get; set; }
        public decimal? SoldAmountBase { get; set; }
    }
}
