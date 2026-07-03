using System.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.ArchitectureTests.TestDoubles;

/// <summary>
/// A connection factory that throws if a connection is ever requested. Used to prove that a
/// tenant-scoped read (e.g. WebCatalogService.GetAvailableParts, VisualPartSearchService's part
/// candidate query) short-circuits on an unresolved-anonymous TenantContext BEFORE reaching the
/// database, instead of accidentally querying with TenantId == 0 (which SQL treats as "all
/// tenants") and leaking cross-tenant data.
/// </summary>
internal sealed class ThrowIfCalledSqlConnectionFactory : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
        => throw new InvalidOperationException(
            "CreateConnection should not be called when the tenant guard should have short-circuited.");
}
