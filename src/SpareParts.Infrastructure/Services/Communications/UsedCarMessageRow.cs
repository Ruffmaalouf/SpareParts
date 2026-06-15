using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class UsedCarMessageRow
    {
        public int Id { get; set; }
        public string Car { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string PriceCurrency { get; set; } = "USD";
        public decimal Price { get; set; }
    }
}
