-- ════════════════════════════════════════════════════════════════════════════
-- Ignition Redesign — Phase 2 — Single-Customer Aging Buckets
-- Source spec : docs/redesign/04-system-architecture.html §05 + §07
--               ("Aging bucket computation (single customer)")
-- Derived from: CustomersService.GetAging() —
--               src/SpareParts.Infrastructure/Services/CustomersService.cs
-- Consumed by : GET /api/clients/{id}/workspace (ClientWorkspaceService)
-- Author      : dev-database · 2026-07-03
--
-- PARAMETER CONTRACT (Dapper anonymous object)
--   @CustomerId  INT — route value {id}. The calling service MUST validate
--                that this customer exists AND belongs to the resolved tenant
--                BEFORE running this query (uniform 404 on mismatch — the SQL
--                predicate below is defence in depth, not the authorization
--                boundary; Report 04 §07/§09).
--   @TenantId    INT — sourced ONLY from ITenantContext.TenantId (JWT
--                tenant_id claim via TenantResolutionMiddleware). NEVER from
--                a request parameter. @TenantId = 0 means SuperAdmin
--                ("no filter"), matching the codebase-wide guard shape.
--
--   Example:
--     conn.QuerySingle<ClientAgingRow>(sql,
--         new { CustomerId = customerId, TenantId = _tenantContext.TenantId });
--
-- RESULT CONTRACT (exactly one row, all DECIMAL(19,4)-compatible)
--   Current · Days1To30 · Days31To60 · Days61To90 · Over90Days · TotalBalance
--   Maps to the "balance" object of the workspace DTO (Report 04 §05).
--
-- FIDELITY NOTES (kept identical to CustomersService.GetAging so the
-- workspace header always agrees with the tenant-wide Customer Aging screen)
--   • Same CTE shape: SaleBalances (TypeKey 'Sale') + CreditNotes
--     (TypeKeys 'CreditNote'/'Refund'), narrowed with the one added
--     @CustomerId predicate per Report 04 §07.
--   • Same bucket math: DATEDIFF(DAY, TransactionDate, SYSUTCDATETIME()),
--     credit notes subtracted from the Current bucket and from TotalBalance.
--   • Same open-balance rule: only sales with Balance > 0 feed the buckets.
--   • ONE deliberate difference: GetAging() drops customers whose net
--     balance is <= 0 (HAVING ... > 0). The workspace must render a header
--     for every customer, so this query always returns one zero-filled row
--     instead of no row.
--
-- INDEX USE (verify with the actual plan after the Phase 0 migration)
--   dbo.Transactions seeks on IX_Transactions_Tenant_Customer
--   (TenantId, CustomerId) INCLUDE (TotalAmount, PaidAmount, TransactionDate)
--   from database/migrations/2026-07-03-001-ignition-covering-indexes.sql.
-- ════════════════════════════════════════════════════════════════════════════

WITH SaleBalances AS (
    SELECT
        t.CustomerId,
        t.TransactionDate,
        ISNULL(t.TotalAmount, 0) - ISNULL(t.PaidAmount, 0) AS Balance
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = N'Sale'
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
),
CreditNotes AS (
    SELECT
        t.CustomerId,
        SUM(ISNULL(t.TotalAmount, 0)) AS TotalCredits
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey IN (N'CreditNote', N'Refund')
      AND t.CustomerId = @CustomerId
      AND (@TenantId = 0 OR t.TenantId = @TenantId)
    GROUP BY t.CustomerId
)
SELECT
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) < 1              THEN sb.Balance ELSE 0 END), 0)
        - ISNULL(MAX(cn.TotalCredits), 0)                                                     AS [Current],
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 1  AND 30 THEN sb.Balance ELSE 0 END), 0) AS Days1To30,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 31 AND 60 THEN sb.Balance ELSE 0 END), 0) AS Days31To60,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) BETWEEN 61 AND 90 THEN sb.Balance ELSE 0 END), 0) AS Days61To90,
    ISNULL(SUM(CASE WHEN DATEDIFF(DAY, sb.TransactionDate, SYSUTCDATETIME()) > 90              THEN sb.Balance ELSE 0 END), 0) AS Over90Days,
    ISNULL(SUM(sb.Balance), 0) - ISNULL(MAX(cn.TotalCredits), 0)                              AS TotalBalance
FROM (SELECT 1 AS Anchor) one                        -- guarantees exactly one row even with no open sales
LEFT JOIN SaleBalances sb ON sb.Balance > 0          -- same open-balance rule as GetAging()
LEFT JOIN CreditNotes  cn ON cn.CustomerId = @CustomerId;
