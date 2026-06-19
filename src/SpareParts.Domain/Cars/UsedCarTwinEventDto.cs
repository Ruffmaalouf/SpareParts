namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarTwinEventDto
    {
        public string EventType { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? AmountBase { get; set; }
        public bool IsManual { get; set; }
    }
}
