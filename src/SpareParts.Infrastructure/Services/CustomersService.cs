using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class CustomersService
{
    private const string CustomerControlAccountCode = "1200";
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public CustomersService(ISqlConnectionFactory factory, ITenantContext? tenantContext = null)
    {
        _factory = factory;
        _tenantContext = tenantContext ?? TenantContext.Legacy;
    }

    public (IEnumerable<CustomerDto> Items, int TotalCount) GetAll(string? search, int page, int pageSize)
    {
        using var session = CreateSession();
        var repository = new CustomersRepository(session);
        return repository.GetPaged(search, page, pageSize);
    }

    public int Create(CreateCustomerRequest request, int userId)
    {
        using var session = CreateSession();
        var repository = new CustomersRepository(session);
        var accounting = RepositoryCatalog.For(session).Accounting;
        var customer = new Customer
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            OpeningBalance = request.OpeningBalance,
            CreditLimit = request.CreditLimit,
            AccountId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(customer);
        var accountId = EnsureCustomerAccount(accounting, id, customer.Name, null, userId);
        repository.SetAccountId(id, accountId, userId);
        session.Commit();
        return id;
    }

    public void Update(int id, CreateCustomerRequest request, int userId)
    {
        using var session = CreateSession();
        var repository = new CustomersRepository(session);
        var accounting = RepositoryCatalog.For(session).Accounting;
        var existing = repository.GetById(id) ?? throw new NotFoundException("Customer not found.");

        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Customer not found.");
        }

        var accountId = EnsureCustomerAccount(accounting, id, request.Name, existing.AccountId, userId);
        repository.SetAccountId(id, accountId, userId);
        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = CreateSession();
        var repository = new CustomersRepository(session);
        var accounting = RepositoryCatalog.For(session).Accounting;
        var existing = repository.GetById(id) ?? throw new NotFoundException("Customer not found.");

        if (!repository.Delete(id))
        {
            throw new NotFoundException("Customer not found.");
        }

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

    public int? GetCustomerAccountId(int customerId)
    {
        using var session = CreateSession();
        return new CustomersRepository(session).GetAccountId(customerId);
    }

    public IEnumerable<CustomerAgingDto> GetAging()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<CustomerAgingDto>(
            """
WITH SaleBalances AS (
    SELECT
        t.CustomerId,
        t.TransactionDate,
        ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0) AS Balance
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = N'Sale'
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
),
CreditNotes AS (
    SELECT
        t.CustomerId,
        SUM(ISNULL(t.TotalAmount, 0)) AS TotalCredits
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey IN (N'CreditNote', N'Refund')
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
    GROUP BY t.CustomerId
)
SELECT
    c.Id                                          AS CustomerId,
    c.Name                                        AS CustomerName,
    c.Phone,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) < 1    THEN sb.Balance ELSE 0 END), 0)
        - ISNULL(MAX(cn.TotalCredits), 0)         AS [Current],
    SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 1  AND 30 THEN sb.Balance ELSE 0 END) AS Days1To30,
    SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 31 AND 60 THEN sb.Balance ELSE 0 END) AS Days31To60,
    SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 61 AND 90 THEN sb.Balance ELSE 0 END) AS Days61To90,
    SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) > 90             THEN sb.Balance ELSE 0 END) AS Over90Days,
    SUM(sb.Balance) - ISNULL(MAX(cn.TotalCredits), 0) AS TotalBalance
FROM dbo.Customers c
INNER JOIN SaleBalances sb ON sb.CustomerId = c.Id
LEFT JOIN CreditNotes cn ON cn.CustomerId = c.Id
WHERE sb.Balance > 0
  AND (@TenantId = 0 OR c.TenantId = @TenantId)
GROUP BY c.Id, c.Name, c.Phone
HAVING SUM(sb.Balance) - ISNULL(MAX(cn.TotalCredits), 0) > 0
ORDER BY SUM(sb.Balance) - ISNULL(MAX(cn.TotalCredits), 0) DESC;
""",
            new { TenantId = _tenantContext.TenantId });
    }

    private DbSession CreateSession() => new(_factory, _tenantContext.TenantId);

    private static int EnsureCustomerAccount(AccountingRepositories accounting, int customerId, string customerName, int? existingAccountId, int userId)
    {
        var parentAccountId = EnsureCustomerControlAccount(accounting.Accounts, userId);
        var accountCode = BuildCustomerAccountCode(customerId);
        var accountName = BuildCustomerAccountName(customerName, customerId);

        if (existingAccountId is > 0)
        {
            var existingAccount = accounting.Accounts.GetById(existingAccountId.Value);
            if (existingAccount != null)
            {
                accounting.Accounts.Update(existingAccount.Id, accountCode, accountName, "asset", parentAccountId, userId);
                return existingAccount.Id;
            }
        }

        var accountByCode = accounting.Accounts.GetByCode(accountCode);
        if (accountByCode != null)
        {
            accounting.Accounts.Update(accountByCode.Id, accountCode, accountName, "asset", parentAccountId, userId);
            return accountByCode.Id;
        }

        return accounting.Accounts.Insert(new Account
        {
            Code = accountCode,
            Name = accountName,
            AccountTypeKey = "asset",
            ParentId = parentAccountId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static int EnsureCustomerControlAccount(SpareParts.Infrastructure.Interfaces.Repositories.IAccountsRepository accountsRepository, int userId)
    {
        var existing = accountsRepository.GetByCode(CustomerControlAccountCode);
        if (existing != null)
        {
            return existing.Id;
        }

        return accountsRepository.Insert(new Account
        {
            Code = CustomerControlAccountCode,
            Name = "Customer Accounts",
            AccountTypeKey = "asset",
            ParentId = null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static string BuildCustomerAccountCode(int customerId)
        => $"CUST-{customerId:D6}";

    private static string BuildCustomerAccountName(string? customerName, int customerId)
    {
        var safeName = string.IsNullOrWhiteSpace(customerName)
            ? $"Customer {customerId}"
            : customerName.Trim();

        return safeName.Length > 149
            ? $"Customer - {safeName[..149]}"
            : $"Customer - {safeName}";
    }
}
