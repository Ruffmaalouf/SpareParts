using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class LosingCarRow
    {
        public int UsedCarId { get; set; }
        public string Car { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal LoadedCost { get; set; }
        public decimal SalesRevenue { get; set; }
        public decimal EstimatedSoldCost { get; set; }
        public int SaleCount { get; set; }
        public DateTime? LastSaleAt { get; set; }
        public decimal RealizedMargin { get; set; }
    }
}
