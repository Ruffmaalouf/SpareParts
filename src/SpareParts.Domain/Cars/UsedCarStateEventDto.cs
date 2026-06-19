namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarStateEventDto
    {
        public int Id { get; set; }
        public int UsedCarId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public decimal? Mileage { get; set; }
        public string? Condition { get; set; }
        public string? Location { get; set; }
        public string? Note { get; set; }
        public DateTime RecordedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
    }
}
