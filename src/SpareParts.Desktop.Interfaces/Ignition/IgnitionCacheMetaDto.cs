namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Cache metadata block returned by the Ignition aggregate endpoints (Report 04 §03/§05).</summary>
    public sealed class IgnitionCacheMetaDto
    {
        public string CacheKey { get; set; } = string.Empty;
        public int TtlSeconds { get; set; }
        public string Etag { get; set; } = string.Empty;
    }
}
