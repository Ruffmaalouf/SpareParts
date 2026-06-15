using Dapper;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class StockMovementFact
    {
        public DateTime? FirstMovementAt { get; set; }
        public int MovementCount { get; set; }
        public int NetQuantity { get; set; }
    }
}
