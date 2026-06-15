using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class LosingPartRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public DateTime? LastSoldAt { get; set; }
    }
}
