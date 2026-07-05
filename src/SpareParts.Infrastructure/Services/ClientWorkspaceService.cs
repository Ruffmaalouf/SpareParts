using Dapper;
using SpareParts.Domain.Clients;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// Backend read model for the Ignition Client Workspace (Report 04 §05–§07):
    /// GET /api/clients/{id}/workspace plus the paged child resources and typeahead search.
    ///
    /// Tenant-isolation invariants (Report 04 §09, CLAUDE.md security focus):
    /// - TenantId comes only from ITenantContext (JWT, server-resolved) — never from the request.
    /// - The explicit customer-ownership check runs BEFORE any child query; a missing or
    ///   cross-tenant customer id throws the same NotFoundException either way, so the
    ///   response is a uniform 404 (enumeration prevention).
    /// - SuperAdmin (TenantId = 0) keeps the codebase's "no filter" semantics.
    /// - No DTO carries tenant ids, database names, or cache keys.
    /// </summary>
    public sealed class ClientWorkspaceService
    {
        public const int DefaultPageSize = 25;
        public const int MaxPageSize = 100;
        private const int SearchDefaultTake = 10;
        private const int SearchMaxTake = 25;
        private const string SalePaymentReferenceType = "SalePayment";
        private const string CustomerRecipientKind = "Customer";
        private static readonly string[] TimelineEventTypes = ["invoice", "payment", "quote", "message"];
        private static readonly string[] ActivePartRequestStatuses = ["Open", "Contacted", "Reserved"];

        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public ClientWorkspaceService(ISqlConnectionFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public ClientWorkspaceDto GetWorkspace(int customerId)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            var customer = LoadOwnedCustomer(session, customerId);
            var hasLoyaltyPoints = AccountingSchemaInspector.HasColumn(session, "dbo.Customers", "LoyaltyPoints");
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            var balance = LoadBalance(session, customerId);
            var kpis = LoadKpis(session, customerId, hasLoyaltyPoints);
            var lastActivityAt = LoadLastActivityAt(session, customerId);

            session.Commit();

            return new ClientWorkspaceDto
            {
                CustomerId = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                IsActive = true,
                CurrencyCode = currencyContext.CounterCurrencyCode,
                Balance = balance,
                Kpis = kpis,
                Vehicles = new List<ClientVehicleDto>(),
                LastActivityAt = lastActivityAt
            };
        }

        public (IReadOnlyList<ClientInvoiceDto> Items, int TotalCount) GetInvoices(
            int customerId,
            int page,
            int pageSize,
            string? sort,
            string? status)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            LoadOwnedCustomer(session, customerId);

            var orderBy = string.Equals(sort, "amount_desc", StringComparison.OrdinalIgnoreCase)
                ? "t.TotalAmount DESC, t.TransactionDate DESC, t.ReferenceId DESC"
                : "t.TransactionDate DESC, t.ReferenceId DESC";
            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();

            var sql = $@"
SELECT COUNT(1)
FROM dbo.Transactions t
INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
WHERE t.CustomerId = @CustomerId
  AND (@Status IS NULL OR t.PaymentStatus = @Status)
  AND (@TenantId = 0 OR t.TenantId = @TenantId);

SELECT
    t.ReferenceId AS InvoiceId,
    t.TransactionNumber AS InvoiceNumber,
    t.TransactionDate AS InvoiceDate,
    ISNULL(t.TotalAmount, 0) AS TotalAmount,
    ISNULL(t.PaidAmount, 0) AS PaidAmount,
    ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0) AS RemainingAmount,
    t.PaymentStatus,
    t.IsReturn,
    CurrencyCode = ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD')
FROM dbo.Transactions t
INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
WHERE t.CustomerId = @CustomerId
  AND (@Status IS NULL OR t.PaymentStatus = @Status)
  AND (@TenantId = 0 OR t.TenantId = @TenantId)
ORDER BY {orderBy}
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = session.Connection.QueryMultiple(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    Status = normalizedStatus,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize,
                    session.TenantId
                },
                session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<ClientInvoiceDto>().ToList();
            session.Commit();
            return (items, totalCount);
        }

        public (IReadOnlyList<ClientPaymentDto> Items, int TotalCount) GetPayments(int customerId, int page, int pageSize)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            LoadOwnedCustomer(session, customerId);

            // Sale payments are journal entries with ReferenceType = 'SalePayment' whose
            // ReferenceId is the sale invoice id — the exact rows SalesService.IssuePayment
            // and CreateSaleHandler already write. The cash movement is the debit line total.
            const string sql = @"
WITH CustomerPayments AS
(
    SELECT
        je.Id AS JournalEntryId,
        je.EntryDate AS PaymentDate,
        Amount = (
            SELECT ISNULL(SUM(jl.Debit), 0)
            FROM dbo.JournalLines jl
            WHERE jl.JournalEntryId = je.Id
              AND jl.Debit > 0
              AND (@TenantId = 0 OR jl.TenantId = @TenantId)
        ),
        t.ReferenceId AS InvoiceId,
        t.TransactionNumber AS InvoiceNumber,
        je.Description
    FROM dbo.JournalEntries je
    INNER JOIN dbo.Transactions t
        ON t.ReferenceId = je.ReferenceId
       AND (@TenantId = 0 OR t.TenantId = @TenantId)
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE je.ReferenceType = @SalePaymentReferenceType
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR je.TenantId = @TenantId)
)
SELECT COUNT(1) FROM CustomerPayments;

WITH CustomerPayments AS
(
    SELECT
        je.Id AS JournalEntryId,
        je.EntryDate AS PaymentDate,
        Amount = (
            SELECT ISNULL(SUM(jl.Debit), 0)
            FROM dbo.JournalLines jl
            WHERE jl.JournalEntryId = je.Id
              AND jl.Debit > 0
              AND (@TenantId = 0 OR jl.TenantId = @TenantId)
        ),
        t.ReferenceId AS InvoiceId,
        t.TransactionNumber AS InvoiceNumber,
        je.Description
    FROM dbo.JournalEntries je
    INNER JOIN dbo.Transactions t
        ON t.ReferenceId = je.ReferenceId
       AND (@TenantId = 0 OR t.TenantId = @TenantId)
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE je.ReferenceType = @SalePaymentReferenceType
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR je.TenantId = @TenantId)
)
SELECT JournalEntryId, PaymentDate, Amount, InvoiceId, InvoiceNumber, Description
FROM CustomerPayments
ORDER BY PaymentDate DESC, JournalEntryId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = session.Connection.QueryMultiple(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    SalePaymentReferenceType,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize,
                    session.TenantId
                },
                session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<ClientPaymentDto>().ToList();
            session.Commit();
            return (items, totalCount);
        }

        public (IReadOnlyList<ClientQuoteDto> Items, int TotalCount) GetQuotes(int customerId, int page, int pageSize, string? status)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            LoadOwnedCustomer(session, customerId);

            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            const string sql = @"
SELECT COUNT(1)
FROM dbo.Quotes q
WHERE q.CustomerId = @CustomerId
  AND (@Status IS NULL OR q.Status = @Status)
  AND (@TenantId = 0 OR q.TenantId = @TenantId);

SELECT
    q.Id AS QuoteId,
    q.QuoteNumber,
    q.QuoteDate,
    q.ExpiryDate,
    q.Status,
    TotalAmount = ISNULL(
        (SELECT SUM(qi.Quantity * qi.UnitPrice - qi.DiscountAmount)
         FROM dbo.QuoteItems qi
         WHERE qi.QuoteId = q.Id), 0)
FROM dbo.Quotes q
WHERE q.CustomerId = @CustomerId
  AND (@Status IS NULL OR q.Status = @Status)
  AND (@TenantId = 0 OR q.TenantId = @TenantId)
ORDER BY q.QuoteDate DESC, q.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = session.Connection.QueryMultiple(
                sql,
                new
                {
                    CustomerId = customerId,
                    Status = normalizedStatus,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize,
                    session.TenantId
                },
                session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<ClientQuoteDto>().ToList();
            session.Commit();
            return (items, totalCount);
        }

        public (IReadOnlyList<ClientPartRequestDto> Items, int TotalCount) GetPartRequests(int customerId, int page, int pageSize, string? status)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            LoadOwnedCustomer(session, customerId);

            var normalizedStatus = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
            var onlyActive = string.Equals(normalizedStatus, "Active", StringComparison.OrdinalIgnoreCase);
            var exactStatus = onlyActive ? null : normalizedStatus;

            const string sql = @"
SELECT COUNT(1)
FROM dbo.PartRequests pr
WHERE pr.CustomerId = @CustomerId
  AND (@Status IS NULL OR pr.Status = @Status)
  AND (@OnlyActive = 0 OR pr.Status IN @ActiveStatuses)
  AND (@TenantId = 0 OR pr.TenantId = @TenantId);

SELECT
    pr.Id AS RequestId,
    pr.RequestedPartName,
    pr.RequestedOemNumber,
    pr.VehicleDetails,
    pr.Quantity,
    pr.Status,
    pr.CreatedAt
FROM dbo.PartRequests pr
WHERE pr.CustomerId = @CustomerId
  AND (@Status IS NULL OR pr.Status = @Status)
  AND (@OnlyActive = 0 OR pr.Status IN @ActiveStatuses)
  AND (@TenantId = 0 OR pr.TenantId = @TenantId)
ORDER BY pr.CreatedAt DESC, pr.Id DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = session.Connection.QueryMultiple(
                sql,
                new
                {
                    CustomerId = customerId,
                    Status = exactStatus,
                    OnlyActive = onlyActive ? 1 : 0,
                    ActiveStatuses = ActivePartRequestStatuses,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize,
                    session.TenantId
                },
                session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<ClientPartRequestDto>().ToList();
            session.Commit();
            return (items, totalCount);
        }

        public (IReadOnlyList<ClientTimelineEventDto> Items, int TotalCount) GetTimeline(
            int customerId,
            int page,
            int pageSize,
            IReadOnlyCollection<string>? types)
        {
            GuardResolvedTenant();
            using var session = CreateSession();
            LoadOwnedCustomer(session, customerId);

            var requestedTypes = (types is { Count: > 0 }
                    ? types
                        .Select(type => type.Trim().ToLowerInvariant())
                        .Where(type => TimelineEventTypes.Contains(type, StringComparer.Ordinal))
                        .Distinct()
                        .ToArray()
                    : TimelineEventTypes)
                is { Length: > 0 } filtered
                ? filtered
                : TimelineEventTypes;

            // One UNION ALL, ordered and paged in SQL — never merged client-side (Report 04 §08).
            const string timelineCte = @"
TimelineEvents AS
(
    SELECT
        EventType = N'invoice',
        EventDate = t.TransactionDate,
        Title = t.TransactionNumber,
        Detail = CASE WHEN t.IsReturn = 1 THEN N'Sales return' ELSE N'Sale invoice' END,
        Amount = CAST(ISNULL(t.TotalAmount, 0) AS DECIMAL(19, 4)),
        Status = t.PaymentStatus,
        ReferenceId = t.ReferenceId
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)

    UNION ALL

    SELECT
        EventType = N'payment',
        EventDate = je.EntryDate,
        Title = ISNULL(je.Description, N'Payment received'),
        Detail = t.TransactionNumber,
        Amount = (
            SELECT CAST(ISNULL(SUM(jl.Debit), 0) AS DECIMAL(19, 4))
            FROM dbo.JournalLines jl
            WHERE jl.JournalEntryId = je.Id
              AND jl.Debit > 0
              AND (@TenantId = 0 OR jl.TenantId = @TenantId)
        ),
        Status = N'Posted',
        ReferenceId = t.ReferenceId
    FROM dbo.JournalEntries je
    INNER JOIN dbo.Transactions t
        ON t.ReferenceId = je.ReferenceId
       AND (@TenantId = 0 OR t.TenantId = @TenantId)
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE je.ReferenceType = @SalePaymentReferenceType
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR je.TenantId = @TenantId)

    UNION ALL

    SELECT
        EventType = N'quote',
        EventDate = q.QuoteDate,
        Title = q.QuoteNumber,
        Detail = N'Quote',
        Amount = CAST(ISNULL(
            (SELECT SUM(qi.Quantity * qi.UnitPrice - qi.DiscountAmount)
             FROM dbo.QuoteItems qi
             WHERE qi.QuoteId = q.Id), 0) AS DECIMAL(19, 4)),
        Status = q.Status,
        ReferenceId = q.Id
    FROM dbo.Quotes q
    WHERE q.CustomerId = @CustomerId
      AND (@TenantId = 0 OR q.TenantId = @TenantId)

    UNION ALL

    SELECT
        EventType = N'message',
        EventDate = om.CreatedAt,
        Title = ISNULL(NULLIF(om.TemplateKey, N''), om.Channel),
        Detail = LEFT(ISNULL(om.Body, N''), 200),
        Amount = CAST(NULL AS DECIMAL(19, 4)),
        Status = om.Status,
        ReferenceId = om.Id
    FROM dbo.OutboundMessages om
    WHERE om.RecipientKind = @CustomerRecipientKind
      AND om.RecipientId = @CustomerId
      AND (@TenantId = 0 OR om.TenantId = @TenantId)
)";

            var sql = $@"
WITH {timelineCte}
SELECT COUNT(1) FROM TimelineEvents WHERE EventType IN @Types;

WITH {timelineCte}
SELECT EventType, EventDate, Title, Detail, Amount, Status, ReferenceId
FROM TimelineEvents
WHERE EventType IN @Types
ORDER BY EventDate DESC, EventType, ReferenceId DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = session.Connection.QueryMultiple(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    SalePaymentReferenceType,
                    CustomerRecipientKind,
                    Types = requestedTypes,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize,
                    session.TenantId
                },
                session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<ClientTimelineEventDto>().ToList();
            session.Commit();
            return (items, totalCount);
        }

        public IReadOnlyList<ClientSearchResultDto> Search(string? query, int take)
        {
            // Typeahead is deliberately lenient: short/empty queries return an empty list
            // instead of an error so a keystroke-driven UI never surfaces failures.
            var normalized = query?.Trim();
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 2)
            {
                return Array.Empty<ClientSearchResultDto>();
            }

            if (!_tenantContext.IsSuperAdmin && _tenantContext.TenantId <= 0)
            {
                return Array.Empty<ClientSearchResultDto>();
            }

            take = Math.Clamp(take, 1, SearchMaxTake);

            using var session = CreateSession();
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            // Same LIKE matching CustomersController.GetAll already uses, plus phone,
            // with the customer's open sale balance for the client rail (Report 04 §06).
            const string sql = @"
SELECT TOP (@Take)
    c.Id AS CustomerId,
    c.Name,
    c.Phone,
    Balance = ISNULL(bal.Balance, 0)
FROM dbo.Customers c
OUTER APPLY
(
    SELECT SUM(ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0)) AS Balance
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE t.CustomerId = c.Id
      AND ISNULL(t.TotalAmount, 0) > ISNULL(t.PaidAmount, 0)
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
) bal
WHERE (c.Name LIKE @Search OR ISNULL(c.Phone, N'') LIKE @Search)
  AND (@TenantId = 0 OR c.TenantId = @TenantId)
ORDER BY c.Name, c.Id;";

            var items = session.Connection.Query<ClientSearchResultDto>(
                sql,
                new
                {
                    Take = take,
                    Search = $"%{normalized}%",
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    session.TenantId
                },
                session.Transaction).ToList();

            foreach (var item in items)
            {
                item.CurrencyCode = currencyContext.CounterCurrencyCode;
            }

            session.Commit();
            return items;
        }

        public static (int Page, int PageSize) NormalizeChildPagination(int? page, int? pageSize)
            => (Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));

        public static int NormalizeSearchTake(int? take) => Math.Clamp(take ?? SearchDefaultTake, 1, SearchMaxTake);

        private DbSession CreateSession() => new(_factory, _tenantContext.TenantId);

        /// <summary>
        /// C1-pattern guard: an unresolved/anonymous tenant (TenantId &lt;= 0 without SuperAdmin)
        /// must never fall through to "@TenantId = 0 means all tenants" SQL semantics.
        /// Thrown as NotFoundException so the response is indistinguishable from a missing id.
        /// </summary>
        private void GuardResolvedTenant()
        {
            if (!_tenantContext.IsSuperAdmin && _tenantContext.TenantId <= 0)
            {
                throw new NotFoundException("Customer not found.");
            }
        }

        /// <summary>
        /// Explicit tenant-ownership check, executed before any child query (Report 04 §07).
        /// Missing and cross-tenant ids produce the identical NotFoundException — the SQL
        /// predicate is the filter, and this uniform 404 is the authorization boundary.
        /// </summary>
        private static ClientWorkspaceCustomerRow LoadOwnedCustomer(DbSession session, int customerId)
        {
            var customer = session.Connection.QuerySingleOrDefault<ClientWorkspaceCustomerRow>(
                @"
SELECT c.Id, c.Name, c.Phone, c.Email, c.Address
FROM dbo.Customers c
WHERE c.Id = @CustomerId
  AND (@TenantId = 0 OR c.TenantId = @TenantId);",
                new { CustomerId = customerId, session.TenantId },
                session.Transaction);

            return customer ?? throw new NotFoundException("Customer not found.");
        }

        private static ClientBalanceDto LoadBalance(DbSession session, int customerId)
        {
            // Same CTE shape as CustomersService.GetAging, narrowed with CustomerId (Report 04 §07).
            const string sql = @"
WITH SaleBalances AS (
    SELECT t.CustomerId, t.TransactionDate,
           ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0) AS Balance
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = @SaleTypeKey
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
)
SELECT
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()) < 1  THEN Balance ELSE 0 END), 0) AS [Current],
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()) BETWEEN 1  AND 30 THEN Balance ELSE 0 END), 0) AS Days1To30,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()) BETWEEN 31 AND 60 THEN Balance ELSE 0 END), 0) AS Days31To60,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()) BETWEEN 61 AND 90 THEN Balance ELSE 0 END), 0) AS Days61To90,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()) > 90 THEN Balance ELSE 0 END), 0) AS Over90Days,
    ISNULL(SUM(Balance), 0) AS TotalBalance
FROM SaleBalances
WHERE Balance > 0;";

            return session.Connection.QuerySingle<ClientBalanceDto>(
                sql,
                new { CustomerId = customerId, SaleTypeKey = TransactionTypeKeys.Sale, session.TenantId },
                session.Transaction);
        }

        private static ClientWorkspaceKpisDto LoadKpis(DbSession session, int customerId, bool hasLoyaltyPoints)
        {
            var loyaltySelect = hasLoyaltyPoints
                ? "(SELECT c.LoyaltyPoints FROM dbo.Customers c WHERE c.Id = @CustomerId AND (@TenantId = 0 OR c.TenantId = @TenantId))"
                : "CAST(NULL AS INT)";

            var sql = $@"
SELECT
    LifetimeRevenue = ISNULL((
        SELECT SUM(CASE WHEN t.IsReturn = 1 THEN -ISNULL(t.TotalAmount, 0) ELSE ISNULL(t.TotalAmount, 0) END)
        FROM dbo.Transactions t
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
        WHERE t.CustomerId = @CustomerId
          AND (@TenantId = 0 OR t.TenantId = @TenantId)), 0),
    LifetimeInvoiceCount = ISNULL((
        SELECT COUNT(1)
        FROM dbo.Transactions t
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
        WHERE t.CustomerId = @CustomerId
          AND t.IsReturn = 0
          AND (@TenantId = 0 OR t.TenantId = @TenantId)), 0),
    AverageDaysToPay = (
        SELECT AVG(DATEDIFF(DAY, t.TransactionDate, je.EntryDate))
        FROM dbo.JournalEntries je
        INNER JOIN dbo.Transactions t
            ON t.ReferenceId = je.ReferenceId
           AND (@TenantId = 0 OR t.TenantId = @TenantId)
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
        WHERE je.ReferenceType = @SalePaymentReferenceType
          AND t.CustomerId = @CustomerId
          AND je.EntryDate >= t.TransactionDate
          AND (@TenantId = 0 OR je.TenantId = @TenantId)),
    OpenQuotesCount = ISNULL((
        SELECT COUNT(1)
        FROM dbo.Quotes q
        WHERE q.CustomerId = @CustomerId
          AND q.Status NOT IN (N'Converted', N'Expired', N'Cancelled', N'Rejected')
          AND (@TenantId = 0 OR q.TenantId = @TenantId)), 0),
    ActivePartRequestsCount = ISNULL((
        SELECT COUNT(1)
        FROM dbo.PartRequests pr
        WHERE pr.CustomerId = @CustomerId
          AND pr.Status IN @ActiveStatuses
          AND (@TenantId = 0 OR pr.TenantId = @TenantId)), 0),
    LoyaltyPoints = {loyaltySelect};";

            return session.Connection.QuerySingle<ClientWorkspaceKpisDto>(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    SalePaymentReferenceType,
                    ActiveStatuses = ActivePartRequestStatuses,
                    session.TenantId
                },
                session.Transaction);
        }

        private static DateTime? LoadLastActivityAt(DbSession session, int customerId)
        {
            const string sql = @"
SELECT MAX(EventDate)
FROM
(
    SELECT MAX(t.TransactionDate) AS EventDate
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    WHERE t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)

    UNION ALL

    SELECT MAX(q.QuoteDate)
    FROM dbo.Quotes q
    WHERE q.CustomerId = @CustomerId
      AND (@TenantId = 0 OR q.TenantId = @TenantId)

    UNION ALL

    SELECT MAX(om.CreatedAt)
    FROM dbo.OutboundMessages om
    WHERE om.RecipientKind = @CustomerRecipientKind
      AND om.RecipientId = @CustomerId
      AND (@TenantId = 0 OR om.TenantId = @TenantId)
) events;";

            return session.Connection.ExecuteScalar<DateTime?>(
                sql,
                new
                {
                    CustomerId = customerId,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    CustomerRecipientKind,
                    session.TenantId
                },
                session.Transaction);
        }
    }
}
