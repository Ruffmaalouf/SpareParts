using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class SupplierBalanceRow
    {
        public decimal Balance { get; set; }
        public int LineCount { get; set; }
    }
}
