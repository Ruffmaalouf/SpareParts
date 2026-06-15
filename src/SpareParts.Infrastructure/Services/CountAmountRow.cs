using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class CountAmountRow
    {
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
