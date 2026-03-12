using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    /// <summary>
    /// CarBrand — a car manufacturer (BMW, Toyota, Hyundai …).
    /// Replaces the old hardcoded CarBrand view-model class.
    /// </summary>
    public class CarBrand : AuditableEntity
    {
        public string  Name        { get; set; } = string.Empty;
        public string  Country     { get; set; } = string.Empty;
        /// <summary>German | Japanese | Korean | Other</summary>
        public string  RegionGroup { get; set; } = string.Empty;
        /// <summary>Relative path on disk, e.g. Assets/Logos/bmw.png</summary>
        public string? LogoPath    { get; set; }
        /// <summary>Optional: raw logo bytes stored in DB (overrides LogoPath when set)</summary>
        public byte[]? LogoData    { get; set; }
        public bool    IsActive    { get; set; } = true;
        public int     SortOrder   { get; set; }
    }

    /// <summary>
    /// CarModel — a specific vehicle model belonging to a CarBrand.
    /// Replaces the old CarModel / CarModelEntity duplication.
    /// </summary>
    public class CarModel : AuditableEntity
    {
        public int     CarBrandId  { get; set; }
        public string  Name        { get; set; } = string.Empty;
        public string? Year        { get; set; }
        public string? EngineType  { get; set; }
        public decimal BasePrice   { get; set; }
        /// <summary>Relative path on disk, e.g. Assets/Cars/bmw_m3.png</summary>
        public string? ImagePath   { get; set; }
        /// <summary>Optional: raw image bytes stored in DB</summary>
        public byte[]? ImageData   { get; set; }
        public bool    IsActive    { get; set; } = true;
    }
}
