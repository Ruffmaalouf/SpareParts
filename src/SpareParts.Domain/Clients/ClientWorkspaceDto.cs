namespace SpareParts.Domain.Clients
{
    /// <summary>
    /// Customer-360 read model for GET /api/clients/{id}/workspace (Ignition Report 04 §05).
    /// Header + balance + aging + KPIs only — child collections are paged separately.
    /// Carries no tenant id, database name, or cache metadata (Report 04 §09).
    /// </summary>
    public sealed class ClientWorkspaceDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public string CurrencyCode { get; set; } = "USD";
        public ClientBalanceDto Balance { get; set; } = new();
        public ClientWorkspaceKpisDto Kpis { get; set; } = new();
        public List<ClientVehicleDto> Vehicles { get; set; } = new();
        public DateTime? LastActivityAt { get; set; }
    }
}
