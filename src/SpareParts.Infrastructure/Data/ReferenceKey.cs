using Dapper;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Data
{
    internal readonly record struct ReferenceKey(string ReferenceType, int ReferenceId);
}
