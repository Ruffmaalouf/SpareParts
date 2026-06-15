using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class CustomerCreditRow
    {
        public decimal CreditLimit { get; init; }
        public decimal OutstandingBalance { get; init; }
    }
}
