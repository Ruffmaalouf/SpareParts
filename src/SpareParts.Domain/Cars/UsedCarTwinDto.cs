namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarTwinDto
    {
        public UsedCarDto Car { get; set; } = new();
        public int ImageCount { get; set; }
        public List<UsedCarTwinPartDto> Parts { get; set; } = new();
        public UsedCarWholesaleSaleDto? WholesaleSale { get; set; }
        public List<UsedCarStateEventDto> StateEvents { get; set; } = new();
        public List<UsedCarTwinEventDto> Timeline { get; set; } = new();
    }
}
