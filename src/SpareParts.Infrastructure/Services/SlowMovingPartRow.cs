using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class SlowMovingPartRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal SalePrice { get; set; }
        public string? PartBrandName { get; set; }
        public string? CarBrandName { get; set; }
        public decimal OnHand { get; set; }
        public decimal SoldQuantityLast90 { get; set; }
        public decimal? SoldQuantityAllTime { get; set; }
        public DateTime? LastSoldAt { get; set; }
        public DateTime? LastReceivedAt { get; set; }
        public int? DaysSinceLastSale { get; set; }
    }
}
