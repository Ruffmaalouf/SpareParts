using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class SupplierAssistantRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public int? AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
    }
}
