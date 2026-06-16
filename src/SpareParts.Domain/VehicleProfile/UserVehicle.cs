using SpareParts.Domain.Common;

namespace SpareParts.Domain.VehicleProfile
{
    public class UserVehicle : AuditableEntity
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string? Nickname { get; set; }
        public string? VinNumber { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int Year { get; set; }
        public string? Trim { get; set; }
        public string? EngineCode { get; set; }
        public string? FuelType { get; set; }
        public int? Mileage { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }
}
