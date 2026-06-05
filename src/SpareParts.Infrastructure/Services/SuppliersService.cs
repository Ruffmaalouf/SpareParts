using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;

namespace SpareParts.Infrastructure.Services;

public sealed class SuppliersService
{
    private const string SupplierControlAccountCode = "2100";
    private readonly ISqlConnectionFactory _factory;

    public SuppliersService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public (IEnumerable<SupplierDto> Items, int TotalCount) GetAll(int page, int pageSize)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        return repository.GetPaged(page, pageSize);
    }

    public int Create(CreateSupplierRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        var supplier = new Supplier
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            OpeningBalance = request.OpeningBalance,
            AccountId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(supplier);
        var accounting = RepositoryCatalog.For(session).Accounting;
        var accountId = EnsureSupplierAccount(accounting, id, supplier.Name, null, userId);
        repository.SetAccountId(id, accountId, userId);
        session.Commit();
        return id;
    }

    public void Update(int id, CreateSupplierRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        var existing = repository.GetById(id) ?? throw new NotFoundException("Supplier not found.");
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Supplier not found.");
        }

        var accounting = RepositoryCatalog.For(session).Accounting;
        var accountId = EnsureSupplierAccount(accounting, id, request.Name, existing.AccountId, userId);
        repository.SetAccountId(id, accountId, userId);
        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        var existing = repository.GetById(id) ?? throw new NotFoundException("Supplier not found.");
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Supplier not found.");
        }

        var accounting = RepositoryCatalog.For(session).Accounting;
        if (existing.AccountId is > 0
            && !repository.UsesAccount(existing.AccountId.Value)
            && !accounting.PostingSettings.UsesAccount(existing.AccountId.Value)
            && !accounting.Journal.HasEntriesForAccount(existing.AccountId.Value)
            && !accounting.Accounts.HasChildren(existing.AccountId.Value))
        {
            accounting.Accounts.Delete(existing.AccountId.Value);
        }

        session.Commit();
    }

    public int? GetSupplierAccountId(int supplierId)
    {
        using var session = new DbSession(_factory);
        return new SuppliersRepository(session).GetAccountId(supplierId);
    }

    public IEnumerable<SupplierAgingDto> GetAging()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<SupplierAgingDto>(
            """
SELECT
    s.Id                                          AS SupplierId,
    s.Name                                        AS SupplierName,
    s.Phone,
    SUM(CASE WHEN DATEDIFF(DAY, t.TransactionDate, SYSUTCDATETIME()) < 1    THEN (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) ELSE 0 END) AS Current,
    SUM(CASE WHEN DATEDIFF(DAY, t.TransactionDate, SYSUTCDATETIME()) BETWEEN 1  AND 30 THEN (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) ELSE 0 END) AS Days1To30,
    SUM(CASE WHEN DATEDIFF(DAY, t.TransactionDate, SYSUTCDATETIME()) BETWEEN 31 AND 60 THEN (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) ELSE 0 END) AS Days31To60,
    SUM(CASE WHEN DATEDIFF(DAY, t.TransactionDate, SYSUTCDATETIME()) BETWEEN 61 AND 90 THEN (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) ELSE 0 END) AS Days61To90,
    SUM(CASE WHEN DATEDIFF(DAY, t.TransactionDate, SYSUTCDATETIME()) > 90             THEN (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) ELSE 0 END) AS Over90Days,
    SUM(ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0))                             AS TotalBalance
FROM dbo.Suppliers s
INNER JOIN dbo.Transactions t ON t.SupplierId = s.Id
INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
WHERE tt.TypeKey = N'Purchase'
  AND (ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) > 0
GROUP BY s.Id, s.Name, s.Phone
HAVING SUM(ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) > 0
ORDER BY SUM(ISNULL(t.TotalAmount,0) - ISNULL(t.PaidAmount,0)) DESC;
""");
    }

    private static int EnsureSupplierAccount(AccountingRepositories accounting, int supplierId, string supplierName, int? existingAccountId, int userId)
    {
        var parentAccountId = EnsureSupplierControlAccount(accounting.Accounts, userId);
        var accountCode = BuildSupplierAccountCode(supplierId);
        var accountName = BuildSupplierAccountName(supplierName, supplierId);

        if (existingAccountId is > 0)
        {
            var existingAccount = accounting.Accounts.GetById(existingAccountId.Value);
            if (existingAccount != null)
            {
                accounting.Accounts.Update(existingAccount.Id, accountCode, accountName, "liability", parentAccountId, userId);
                return existingAccount.Id;
            }
        }

        var accountByCode = accounting.Accounts.GetByCode(accountCode);
        if (accountByCode != null)
        {
            accounting.Accounts.Update(accountByCode.Id, accountCode, accountName, "liability", parentAccountId, userId);
            return accountByCode.Id;
        }

        return accounting.Accounts.Insert(new Account
        {
            Code = accountCode,
            Name = accountName,
            AccountTypeKey = "liability",
            ParentId = parentAccountId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static int EnsureSupplierControlAccount(Interfaces.Repositories.IAccountsRepository accounts, int userId)
    {
        var control = accounts.GetByCode(SupplierControlAccountCode);
        if (control != null)
        {
            return control.Id;
        }

        var accountsPayable = accounts.GetByCode("2000");
        var parentId = accountsPayable?.Id;

        return accounts.Insert(new Account
        {
            Code = SupplierControlAccountCode,
            Name = "Supplier Accounts",
            AccountTypeKey = "liability",
            ParentId = parentId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static string BuildSupplierAccountCode(int supplierId)
        => $"SUP-{supplierId:000000}";

    private static string BuildSupplierAccountName(string supplierName, int supplierId)
    {
        var normalizedName = string.IsNullOrWhiteSpace(supplierName)
            ? $"Supplier {supplierId}"
            : supplierName.Trim();

        return normalizedName.Length > 149
            ? $"Supplier - {normalizedName[..149]}"
            : $"Supplier - {normalizedName}";
    }
}
