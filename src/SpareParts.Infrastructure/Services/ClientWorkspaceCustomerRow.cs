namespace SpareParts.Infrastructure.Services
{
    /// <summary>Dapper row for the workspace tenant-ownership check + header identity.</summary>
    public sealed class ClientWorkspaceCustomerRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}
