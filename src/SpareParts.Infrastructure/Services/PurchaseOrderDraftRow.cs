using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Domain.Transactions;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class PurchaseOrderDraftRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int MinStock { get; set; }
        public decimal OnHand { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal SuggestedQuantity { get; set; }
    }
}
