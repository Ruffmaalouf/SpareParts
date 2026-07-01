namespace SpareParts.Domain.Inventory
{
    /// <summary>
    /// A customer waiting for a part — sourced from either an open <c>PartRequest</c> or a
    /// <c>PartWantedAd</c> (need board) entry whose name matches a newly-available part.
    /// </summary>
    public sealed class DemandMatchDto
    {
        public int SourceId { get; set; }

        /// <summary>Either <c>"PartRequest"</c> or <c>"NeedBoard"</c>.</summary>
        public string SourceType { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
