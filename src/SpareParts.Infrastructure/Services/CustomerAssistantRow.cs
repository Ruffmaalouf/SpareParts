using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class CustomerAssistantRow
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal OpenBalance { get; set; }
        public int OpenInvoiceCount { get; set; }
        public int SaleCount { get; set; }
        public decimal TotalSales { get; set; }
        public DateTime? LastSaleAt { get; set; }
    }
}
