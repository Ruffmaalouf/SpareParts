namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitExpenseBreakdownRowDto
    {
        public string Category { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int EntryCount { get; set; }
    }
}
