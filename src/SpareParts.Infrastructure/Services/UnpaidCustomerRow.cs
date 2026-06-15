using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class UnpaidCustomerRow
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal RemainingAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int InvoiceCount { get; set; }
        public DateTime? OldestInvoiceDate { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
    }
}
