namespace SpareParts.Domain.Clients
{
    /// <summary>
    /// Vehicle linked to a client. The current schema only links vehicles to app users
    /// (dbo.UserVehicles.UserId), not to customers, so this list is empty today; the type
    /// exists so the workspace wire contract (Report 04 §05) is stable when a
    /// customer-vehicle link ships.
    /// </summary>
    public sealed class ClientVehicleDto
    {
        public int VehicleId { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? PlateNumber { get; set; }
    }
}
