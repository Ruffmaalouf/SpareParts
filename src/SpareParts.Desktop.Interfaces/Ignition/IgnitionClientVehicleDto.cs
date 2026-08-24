namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Vehicle-on-file row of GET /api/clients/{id}/workspace (Report 04 §05).</summary>
    public sealed class IgnitionClientVehicleDto
    {
        public int VehicleId { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
    }
}
