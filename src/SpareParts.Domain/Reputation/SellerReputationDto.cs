namespace SpareParts.Domain.Reputation
{
    public class SellerReputationDto
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public decimal ReputationScore { get; set; }
        public int ReviewCount { get; set; }
        public int AverageResponseTimeMinutes { get; set; }
        public decimal FulfillmentRate { get; set; }
        public decimal DisputeRate { get; set; }
        public decimal ReturnRate { get; set; }
        public bool IsVerified { get; set; }
        public List<string> Badges { get; set; } = new();
        public DateTime? LastCalculatedAt { get; set; }
    }
}
