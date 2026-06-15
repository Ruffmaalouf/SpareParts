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
    internal sealed record ResolvedRecipient(string Kind, int? Id, string Name, string? Phone);
}
