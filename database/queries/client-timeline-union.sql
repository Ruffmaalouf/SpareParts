-- ════════════════════════════════════════════════════════════════════════════
-- Ignition Redesign — Phase 2 — Client Timeline (Unified, Paged in SQL)
-- Source spec : docs/redesign/04-system-architecture.html §06 (timeline child
--               route, "UNION ALL over the above, ordered by event date,
--               itself paged in SQL — not merged client-side") + §08 (N+1
--               rule) + §09 (tenant guards).
-- Consumed by : GET /api/clients/{id}/timeline (ClientTimelineService)
-- Author      : dev-database · 2026-07-03
--
-- Unifies five event sources for one customer into a single reverse-
-- chronological, SQL-paged stream:
--   invoice       dbo.Transactions (TypeKey 'Sale', incl. returns)
--   payment       dbo.JournalEntries ReferenceType 'SalePayment' — the way
--                 sale payments are actually recorded today (SalesService.
--                 IssuePayment / CreateSaleHandler; there is no customer-
--                 payments table). Amount = credit posted to the customer's
--                 receivable account (Customers.AccountId).
--   quote         dbo.Quotes
--   part-request  dbo.PartRequests
--   message       dbo.OutboundMessages (RecipientKind 'Customer'; Report 04
--                 §06 timeline contract includes type 'message')
--
-- PARAMETER CONTRACT (Dapper anonymous object)
--   @CustomerId          INT — route value {id}. The calling service MUST
--                        verify tenant ownership of the customer BEFORE this
--                        query runs (uniform 404; SQL predicates are defence
--                        in depth, not the authorization boundary — §09).
--   @TenantId            INT — ONLY from ITenantContext.TenantId (JWT claim
--                        via TenantResolutionMiddleware); never a request
--                        input. 0 = SuperAdmin = no tenant filter (codebase
--                        guard shape: (@TenantId = 0 OR x.TenantId = @TenantId)).
--   @IncludeInvoices     BIT ─┐
--   @IncludePayments     BIT  │ map the route's ?types=invoice,payment,...
--   @IncludeQuotes       BIT  │ filter; pass 1 for every type when the
--   @IncludePartRequests BIT  │ parameter is omitted.
--   @IncludeMessages     BIT ─┘
--   @Offset              INT — (page - 1) * pageSize; page/pageSize come from
--                        NormalizeOptionalPagination (default 25, cap 100 for
--                        workspace routes per Report 04 §06).
--   @PageSize            INT
--
--   Example:
--     conn.Query<ClientTimelineEventRow>(sql, new {
--         CustomerId = customerId, TenantId = _tenantContext.TenantId,
--         IncludeInvoices = true, IncludePayments = true, IncludeQuotes = true,
--         IncludePartRequests = true, IncludeMessages = true,
--         Offset = (page - 1) * pageSize, PageSize = pageSize });
--
-- RESULT CONTRACT (one row per event, newest first)
--   EventType    NVARCHAR — 'invoice' | 'payment' | 'quote' | 'part-request'
--                           | 'message' (matches §06 types= vocabulary)
--   EventId      INT      — PK in the source table (deep-link target)
--   EventDate    DATETIME2
--   EventNumber  NVARCHAR — human reference (invoice/quote number), or NULL
--   Title        NVARCHAR — display line
--   Amount       DECIMAL  — money at stake, NULL where not applicable
--   Status       NVARCHAR — source-table status vocabulary
--   TotalCount   INT      — same value on every row (COUNT(*) OVER()); feed
--                           X-Total-Count via ApplyPaginationHeaders
--
-- TENANT-GUARD NOTES (§09 review, signed dev-database)
--   • Transactions / JournalEntries / PartRequests / OutboundMessages carry
--     their own TenantId (added + backfilled by TenantIdMigration) and are
--     guarded directly.
--   • Quotes: QuotesService.Create does not stamp TenantId (backfill happens
--     on the next API startup), so the quote block anchors tenancy on the
--     guarded dbo.Customers join instead — consistent with how QuotesService
--     itself guards through a related table rather than q.TenantId.
--   • Every block also filters CustomerId = @CustomerId, and the service-
--     level ownership check has already pinned that customer to the tenant.
--
-- INDEX USE (verify plans after the Phase 0 migration)
--   invoice/payment blocks: IX_Transactions_Tenant_Customer;
--   messages: IX_OutboundMessages_CreatedAt / RecipientPhone (existing);
--   part-requests: IX_PartRequests_Status_CreatedAt (existing).
-- ════════════════════════════════════════════════════════════════════════════

WITH TimelineEvents AS (

    -- ── invoice — sales (and sales returns) for this customer ──────────────
    SELECT
        N'invoice'                                   AS EventType,
        t.Id                                         AS EventId,
        t.TransactionDate                            AS EventDate,
        t.TransactionNumber                          AS EventNumber,
        CASE WHEN t.IsReturn = 1
             THEN N'Sales return ' + t.TransactionNumber
             ELSE N'Invoice '      + t.TransactionNumber
        END                                          AS Title,
        CASE WHEN t.IsReturn = 1
             THEN -ISNULL(t.TotalAmount, 0)
             ELSE  ISNULL(t.TotalAmount, 0)
        END                                          AS Amount,
        ISNULL(t.PaymentStatus, N'Unpaid')           AS Status
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE @IncludeInvoices = 1
      AND tt.TypeKey = N'Sale'
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)

    UNION ALL

    -- ── payment — SalePayment journal entries crediting the customer's
    --    receivable account (how payments are recorded today; see header) ───
    SELECT
        N'payment'                                   AS EventType,
        je.Id                                        AS EventId,
        je.EntryDate                                 AS EventDate,
        t.TransactionNumber                          AS EventNumber,
        ISNULL(je.Description,
               N'Payment on ' + t.TransactionNumber) AS Title,
        SUM(ISNULL(jl.Credit, 0))                    AS Amount,
        N'Posted'                                    AS Status
    FROM dbo.JournalEntries je
    INNER JOIN dbo.Transactions t
        ON t.ReferenceId = je.ReferenceId
    INNER JOIN dbo.TransactionTypes tt
        ON tt.Id = t.TransactionTypeId AND tt.TypeKey = N'Sale'
    INNER JOIN dbo.Customers c
        ON c.Id = t.CustomerId
    INNER JOIN dbo.JournalLines jl
        ON jl.JournalEntryId = je.Id AND jl.AccountId = c.AccountId
    WHERE @IncludePayments = 1
      AND je.ReferenceType = N'SalePayment'
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId  = @TenantId)
      AND (@TenantId = 0 OR je.TenantId = @TenantId)
    GROUP BY je.Id, je.EntryDate, je.Description, t.TransactionNumber

    UNION ALL

    -- ── quote — tenancy anchored on the guarded Customers join (see header) ─
    SELECT
        N'quote'                                     AS EventType,
        q.Id                                         AS EventId,
        q.QuoteDate                                  AS EventDate,
        q.QuoteNumber                                AS EventNumber,
        N'Quote ' + q.QuoteNumber                    AS Title,
        ISNULL((SELECT SUM(qi.Quantity * qi.UnitPrice - qi.DiscountAmount)
                FROM dbo.QuoteItems qi
                WHERE qi.QuoteId = q.Id), 0)         AS Amount,
        q.Status                                     AS Status
    FROM dbo.Quotes q
    INNER JOIN dbo.Customers c
        ON c.Id = q.CustomerId
       AND (@TenantId = 0 OR c.TenantId = @TenantId)
    WHERE @IncludeQuotes = 1
      AND q.CustomerId = @CustomerId

    UNION ALL

    -- ── part-request ────────────────────────────────────────────────────────
    SELECT
        N'part-request'                              AS EventType,
        pr.Id                                        AS EventId,
        pr.CreatedAt                                 AS EventDate,
        CAST(NULL AS NVARCHAR(50))                   AS EventNumber,
        N'Part request: ' + pr.RequestedPartName     AS Title,
        CAST(NULL AS DECIMAL(19, 4))                 AS Amount,
        pr.Status                                    AS Status
    FROM dbo.PartRequests pr
    WHERE @IncludePartRequests = 1
      AND pr.CustomerId = @CustomerId
      AND (@TenantId = 0 OR pr.TenantId = @TenantId)

    UNION ALL

    -- ── message — outbound/inbound communications for this customer ─────────
    SELECT
        N'message'                                   AS EventType,
        om.Id                                        AS EventId,
        ISNULL(om.SentAt, om.CreatedAt)              AS EventDate,
        CAST(NULL AS NVARCHAR(50))                   AS EventNumber,
        om.Channel + N' ' + LOWER(om.Direction)
            + N' — ' + om.TemplateKey                AS Title,
        CAST(NULL AS DECIMAL(19, 4))                 AS Amount,
        om.Status                                    AS Status
    FROM dbo.OutboundMessages om
    WHERE @IncludeMessages = 1
      AND om.RecipientKind = N'Customer'
      AND om.RecipientId = @CustomerId
      AND (@TenantId = 0 OR om.TenantId = @TenantId)
)
SELECT
    EventType,
    EventId,
    EventDate,
    EventNumber,
    Title,
    Amount,
    Status,
    COUNT(*) OVER () AS TotalCount
FROM TimelineEvents
ORDER BY EventDate DESC, EventId DESC, EventType
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
