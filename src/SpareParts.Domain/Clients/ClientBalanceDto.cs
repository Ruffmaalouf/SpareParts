namespace SpareParts.Domain.Clients
{
    /// <summary>
    /// Single-customer balance with aging buckets — the same bucket shape as the
    /// tenant-wide CustomerAgingDto, narrowed to one customer (Report 04 §07).
    /// </summary>
    public sealed class ClientBalanceDto
    {
        public decimal TotalBalance { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
    }
}
