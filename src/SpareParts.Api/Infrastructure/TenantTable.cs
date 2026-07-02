namespace SpareParts.Api.Infrastructure;

/// <summary>A tenant-owned table that <see cref="TenantIdMigration"/> ensures has a TenantId column (and, optionally, an index on it).</summary>
internal sealed record TenantTable(string Name, bool CreateIndex);
