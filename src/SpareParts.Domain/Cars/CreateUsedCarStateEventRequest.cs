namespace SpareParts.Domain.Cars
{
    public sealed class CreateUsedCarStateEventRequest
    {
        public string EventType { get; set; } = string.Empty;
        public decimal? Mileage { get; set; }
        public string? Condition { get; set; }
        public string? Location { get; set; }
        public string? Note { get; set; }
        public DateTime? RecordedAt { get; set; }
    }
}
