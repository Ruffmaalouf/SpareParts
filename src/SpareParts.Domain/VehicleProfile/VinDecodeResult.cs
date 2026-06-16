namespace SpareParts.Domain.VehicleProfile
{
    public class VinDecodeResult
    {
        public bool Success { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? Trim { get; set; }
        public string? EngineCode { get; set; }
        public string? FuelType { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
