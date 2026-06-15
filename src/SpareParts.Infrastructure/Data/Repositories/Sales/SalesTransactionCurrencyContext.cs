using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class SalesTransactionCurrencyContext
    {
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
    }
}
