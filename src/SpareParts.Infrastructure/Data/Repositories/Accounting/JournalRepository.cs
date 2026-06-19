using Dapper;
using SpareParts.Domain.Accounting;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class JournalRepository : IJournalRepository
    {
        private readonly DbSession _session;
        private AccountingCurrencyContext? _currencyContext;

        public JournalRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertEntry(JournalEntry entry)
        {
            const string sql = @"INSERT INTO JournalEntries
                (EntryDate, ReferenceType, ReferenceId, Description, CreatedAt, CreatedByUserId, TenantId)
                VALUES
                (@EntryDate, @ReferenceType, @ReferenceId, @Description, @CreatedAt, @CreatedByUserId, @TenantId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            return _session.Connection.ExecuteScalar<int>(sql, new
            {
                entry.EntryDate,
                entry.ReferenceType,
                entry.ReferenceId,
                entry.Description,
                entry.CreatedAt,
                entry.CreatedByUserId,
                TenantId = tenantId
            }, _session.Transaction);
        }

        public void InsertLines(int entryId, IList<JournalLine> lines)
        {
            const string sql = @"INSERT INTO JournalLines
                (JournalEntryId, AccountId, Debit, Credit, CurrencyCode, OriginalAmount, RateToBase, CounterAmount, BaseCurrencyCode, CounterCurrencyCode, CreatedAt, CreatedByUserId, TenantId)
                VALUES
                (@JournalEntryId, @AccountId, @Debit, @Credit, @CurrencyCode, @OriginalAmount, @RateToBase, @CounterAmount, @BaseCurrencyCode, @CounterCurrencyCode, @CreatedAt, @CreatedByUserId, @TenantId);";

            var currencyContext = ResolveCurrencyContext();
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            foreach (var line in lines)
            {
                line.JournalEntryId = entryId;
                ApplyCurrencyContext(line, currencyContext);
                _session.Connection.Execute(sql, new
                {
                    line.JournalEntryId,
                    line.AccountId,
                    line.Debit,
                    line.Credit,
                    line.CurrencyCode,
                    line.OriginalAmount,
                    line.RateToBase,
                    line.CounterAmount,
                    line.BaseCurrencyCode,
                    line.CounterCurrencyCode,
                    line.CreatedAt,
                    line.CreatedByUserId,
                    TenantId = tenantId
                }, _session.Transaction);
            }
        }

        public void DeleteEntriesByReference(string referenceType, int referenceId)
        {
            const string deleteLinesSql = @"DELETE jl
                FROM JournalLines jl
                INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                WHERE je.ReferenceType = @ReferenceType AND je.ReferenceId = @ReferenceId
                  AND (@TenantId = 0 OR je.TenantId = @TenantId);";

            const string deleteEntriesSql = @"DELETE FROM JournalEntries
                WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId
                  AND (@TenantId = 0 OR TenantId = @TenantId);";

            var args = new { ReferenceType = referenceType, ReferenceId = referenceId, _session.TenantId };
            _session.Connection.Execute(deleteLinesSql, args, _session.Transaction);
            _session.Connection.Execute(deleteEntriesSql, args, _session.Transaction);
        }

        public IReadOnlyList<JournalEntrySummaryDto> GetEntries(DateTime? dateFrom, DateTime? dateTo)
        {
            const string sql = @"SELECT je.Id,
                                        je.EntryDate,
                                        je.ReferenceType,
                                        je.ReferenceId,
                                        je.Description,
                                        ISNULL(SUM(jl.Debit), 0) AS TotalDebit,
                                        ISNULL(SUM(jl.Credit), 0) AS TotalCredit,
                                        COUNT(jl.Id) AS LineCount,
                                        je.CreatedAt,
                                        je.CreatedByUserId
                                 FROM JournalEntries je
                                 LEFT JOIN JournalLines jl ON jl.JournalEntryId = je.Id
                                 WHERE (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                   AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                   AND (@TenantId = 0 OR je.TenantId = @TenantId)
                                 GROUP BY je.Id, je.EntryDate, je.ReferenceType, je.ReferenceId, je.Description, je.CreatedAt, je.CreatedByUserId
                                 ORDER BY je.EntryDate DESC, je.Id DESC;";

            return _session.Connection.Query<JournalEntrySummaryDto>(sql, new { DateFrom = dateFrom, DateTo = dateTo, _session.TenantId }, _session.Transaction).ToList();
        }

        public JournalEntryDetailDto? GetEntryDetail(int id)
        {
            const string headerSql = @"SELECT je.Id,
                                              je.EntryDate,
                                              je.ReferenceType,
                                              je.ReferenceId,
                                              je.Description,
                                              ISNULL(SUM(jl.Debit), 0) AS TotalDebit,
                                              ISNULL(SUM(jl.Credit), 0) AS TotalCredit,
                                              je.CreatedAt,
                                              je.CreatedByUserId
                                       FROM JournalEntries je
                                       LEFT JOIN JournalLines jl ON jl.JournalEntryId = je.Id
                                       WHERE je.Id = @Id
                                         AND (@TenantId = 0 OR je.TenantId = @TenantId)
                                       GROUP BY je.Id, je.EntryDate, je.ReferenceType, je.ReferenceId, je.Description, je.CreatedAt, je.CreatedByUserId;";

            const string linesSql = @"SELECT jl.AccountId,
                                             a.Code AS AccountCode,
                                             a.Name AS AccountName,
                                             jl.Debit,
                                             jl.Credit
                                      FROM JournalLines jl
                                      INNER JOIN Accounts a ON a.Id = jl.AccountId
                                      WHERE jl.JournalEntryId = @Id
                                        AND (@TenantId = 0 OR jl.TenantId = @TenantId)
                                      ORDER BY a.Code, a.Name;";

            var detail = _session.Connection.QueryFirstOrDefault<JournalEntryDetailDto>(headerSql, new { Id = id, _session.TenantId }, _session.Transaction);
            if (detail == null)
            {
                return null;
            }

            detail.Lines = _session.Connection.Query<JournalEntryLineDto>(linesSql, new { Id = id, _session.TenantId }, _session.Transaction).ToList();
            return detail;
        }

        public IReadOnlyList<LedgerRowDto> GetLedgerRows(int accountId, DateTime? dateFrom, DateTime? dateTo)
        {
            var currencyContext = ResolveCurrencyContext();
            const string sql = @"SELECT je.Id AS JournalEntryId,
                                        je.EntryDate,
                                        je.ReferenceType,
                                        je.ReferenceId,
                                        je.Description,
                                        ISNULL(jl.BaseCurrencyCode, @BaseCurrencyCode) AS BaseCurrencyCode,
                                        ISNULL(jl.CounterCurrencyCode, @CounterCurrencyCode) AS CounterCurrencyCode,
                                        ISNULL(jl.CurrencyCode, ISNULL(jl.BaseCurrencyCode, @BaseCurrencyCode)) AS CurrencyCode,
                                        ISNULL(jl.OriginalAmount, CASE WHEN jl.Debit > 0 THEN jl.Debit ELSE jl.Credit END) AS OriginalAmount,
                                        jl.Debit,
                                        jl.Credit,
                                        CASE WHEN jl.Debit > 0 THEN ISNULL(jl.CounterAmount, 0) ELSE 0 END AS CounterDebit,
                                        CASE WHEN jl.Credit > 0 THEN ISNULL(jl.CounterAmount, 0) ELSE 0 END AS CounterCredit,
                                        CAST(0 AS DECIMAL(19,4)) AS RunningBalance,
                                        CAST(0 AS DECIMAL(19,4)) AS RunningCounterBalance
                                 FROM JournalLines jl
                                 INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                                 WHERE jl.AccountId = @AccountId
                                   AND (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                   AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                   AND (@TenantId = 0 OR jl.TenantId = @TenantId)
                                 ORDER BY je.EntryDate, je.Id, jl.Id;";

            return _session.Connection.Query<LedgerRowDto>(sql, new
            {
                AccountId = accountId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                BaseCurrencyCode = currencyContext.BaseCurrencyCode,
                CounterCurrencyCode = currencyContext.CounterCurrencyCode,
                _session.TenantId
            }, _session.Transaction).ToList();
        }

        public decimal GetOpeningBalance(int accountId, DateTime? dateFrom)
        {
            if (dateFrom == null)
            {
                return 0m;
            }

            const string sql = @"SELECT ISNULL(SUM(jl.Debit - jl.Credit), 0)
                                 FROM JournalLines jl
                                 INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                                 WHERE jl.AccountId = @AccountId
                                   AND je.EntryDate < @DateFrom
                                   AND (@TenantId = 0 OR jl.TenantId = @TenantId);";

            return _session.Connection.ExecuteScalar<decimal>(sql, new { AccountId = accountId, DateFrom = dateFrom, _session.TenantId }, _session.Transaction);
        }

        public decimal GetOpeningCounterBalance(int accountId, DateTime? dateFrom)
        {
            if (dateFrom == null)
            {
                return 0m;
            }

            const string sql = @"SELECT ISNULL(SUM(CASE
                                                     WHEN jl.Debit > 0 THEN ISNULL(jl.CounterAmount, 0)
                                                     ELSE -ISNULL(jl.CounterAmount, 0)
                                                 END), 0)
                                 FROM JournalLines jl
                                 INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                                 WHERE jl.AccountId = @AccountId
                                   AND je.EntryDate < @DateFrom
                                   AND (@TenantId = 0 OR jl.TenantId = @TenantId);";

            return _session.Connection.ExecuteScalar<decimal>(sql, new { AccountId = accountId, DateFrom = dateFrom, _session.TenantId }, _session.Transaction);
        }

        public IReadOnlyList<TrialBalanceRowDto> GetTrialBalanceRows(DateTime? dateFrom, DateTime? dateTo)
        {
            var currencyContext = ResolveCurrencyContext();
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasAccountTypeLookup = AccountingSchemaInspector.HasTable(_session, "dbo.AccountingAccountTypes");
            var accountTypeSource = hasAccountTypeKey ? "a.AccountTypeKey" : "a.AccountType";
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(accountTypeSource);
            var accountTypeLabel = hasAccountTypeLookup
                ? $"ISNULL(t.Label, {AccountingSql.AccountTypeLabel(normalizedAccountTypeKey)})"
                : AccountingSql.AccountTypeLabel(normalizedAccountTypeKey);
            var sql = $@"WITH AccountTotals AS
                         (
                             SELECT a.Id AS AccountId,
                                    a.Code AS AccountCode,
                                    a.Name AS AccountName,
                                    {normalizedAccountTypeKey} AS AccountTypeKey,
                                    {accountTypeLabel} AS AccountType,
                                    COALESCE(MAX(NULLIF(jl.BaseCurrencyCode, '')), @BaseCurrencyCode) AS BaseCurrencyCode,
                                    COALESCE(MAX(NULLIF(jl.CounterCurrencyCode, '')), @CounterCurrencyCode) AS CounterCurrencyCode,
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                        THEN jl.Debit ELSE 0 END), 0) AS TotalDebit,
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                        THEN jl.Credit ELSE 0 END), 0) AS TotalCredit,
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                         AND jl.Debit > 0
                                        THEN ISNULL(jl.CounterAmount, 0) ELSE 0 END), 0) AS TotalCounterDebit,
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                         AND jl.Credit > 0
                                        THEN ISNULL(jl.CounterAmount, 0) ELSE 0 END), 0) AS TotalCounterCredit
                             FROM Accounts a
                             {(hasAccountTypeLookup ? $"LEFT JOIN AccountingAccountTypes t ON t.TypeKey = {normalizedAccountTypeKey}" : string.Empty)}
                             LEFT JOIN JournalLines jl ON jl.AccountId = a.Id AND (@TenantId = 0 OR jl.TenantId = @TenantId)
                             LEFT JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                             WHERE (@TenantId = 0 OR a.TenantId = @TenantId)
                             GROUP BY a.Id, a.Code, a.Name, {normalizedAccountTypeKey}{(hasAccountTypeLookup ? ", t.Label" : string.Empty)}
                         )
                         SELECT AccountId,
                                AccountCode,
                                AccountName,
                                AccountTypeKey,
                                AccountType,
                                BaseCurrencyCode,
                                CounterCurrencyCode,
                                TotalDebit,
                                TotalCredit,
                                CASE WHEN TotalDebit >= TotalCredit THEN TotalDebit - TotalCredit ELSE 0 END AS DebitBalance,
                                CASE WHEN TotalCredit > TotalDebit THEN TotalCredit - TotalDebit ELSE 0 END AS CreditBalance,
                                TotalCounterDebit,
                                TotalCounterCredit,
                                CASE WHEN TotalCounterDebit >= TotalCounterCredit THEN TotalCounterDebit - TotalCounterCredit ELSE 0 END AS CounterDebitBalance,
                                CASE WHEN TotalCounterCredit > TotalCounterDebit THEN TotalCounterCredit - TotalCounterDebit ELSE 0 END AS CounterCreditBalance
                         FROM AccountTotals
                         WHERE TotalDebit <> 0 OR TotalCredit <> 0
                         ORDER BY AccountCode, AccountName;";

            return _session.Connection.Query<TrialBalanceRowDto>(sql, new
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                BaseCurrencyCode = currencyContext.BaseCurrencyCode,
                CounterCurrencyCode = currencyContext.CounterCurrencyCode,
                _session.TenantId
            }, _session.Transaction).ToList();
        }

        public bool HasEntriesForAccount(int accountId)
        {
            const string sql = "SELECT COUNT(1) FROM JournalLines WHERE AccountId = @AccountId AND (@TenantId = 0 OR TenantId = @TenantId);";
            return _session.Connection.ExecuteScalar<int>(sql, new { AccountId = accountId, _session.TenantId }, _session.Transaction) > 0;
        }

        private AccountingCurrencyContext ResolveCurrencyContext()
            => _currencyContext ??= AccountingCurrencyContextResolver.Resolve(_session);

        private static void ApplyCurrencyContext(JournalLine line, AccountingCurrencyContext context)
        {
            var baseAmount = decimal.Round(line.Debit > 0m ? line.Debit : line.Credit, 4, MidpointRounding.AwayFromZero);
            var baseCurrencyCode = NormalizeCurrencyCode(line.BaseCurrencyCode) ?? context.BaseCurrencyCode;
            var counterCurrencyCode = NormalizeCurrencyCode(line.CounterCurrencyCode) ?? context.CounterCurrencyCode;
            var currencyCode = NormalizeCurrencyCode(line.CurrencyCode) ?? baseCurrencyCode;

            var originalAmount = line.OriginalAmount > 0m
                ? decimal.Round(line.OriginalAmount, 4, MidpointRounding.AwayFromZero)
                : baseAmount;

            var rateToBase = line.RateToBase > 0m
                ? decimal.Round(line.RateToBase, 8, MidpointRounding.AwayFromZero)
                : ResolveRateToBase(currencyCode, baseCurrencyCode, counterCurrencyCode, context.CounterRateToBase, baseAmount, originalAmount);

            var counterAmount = line.CounterAmount > 0m
                ? decimal.Round(line.CounterAmount, 4, MidpointRounding.AwayFromZero)
                : ResolveCounterAmount(currencyCode, counterCurrencyCode, baseCurrencyCode, originalAmount, baseAmount, context.CounterRateToBase);

            line.CurrencyCode = currencyCode;
            line.OriginalAmount = originalAmount;
            line.RateToBase = rateToBase > 0m ? rateToBase : 1m;
            line.CounterAmount = counterAmount;
            line.BaseCurrencyCode = baseCurrencyCode;
            line.CounterCurrencyCode = counterCurrencyCode;
        }

        private static decimal ResolveRateToBase(
            string currencyCode,
            string baseCurrencyCode,
            string counterCurrencyCode,
            decimal counterRateToBase,
            decimal baseAmount,
            decimal originalAmount)
        {
            if (string.Equals(currencyCode, baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            if (originalAmount > 0m && baseAmount > 0m)
            {
                return decimal.Round(baseAmount / originalAmount, 8, MidpointRounding.AwayFromZero);
            }

            if (string.Equals(currencyCode, counterCurrencyCode, StringComparison.OrdinalIgnoreCase) && counterRateToBase > 0m)
            {
                return decimal.Round(counterRateToBase, 8, MidpointRounding.AwayFromZero);
            }

            return 1m;
        }

        private static decimal ResolveCounterAmount(
            string currencyCode,
            string counterCurrencyCode,
            string baseCurrencyCode,
            decimal originalAmount,
            decimal baseAmount,
            decimal counterRateToBase)
        {
            if (string.Equals(currencyCode, counterCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return decimal.Round(originalAmount > 0m ? originalAmount : baseAmount, 4, MidpointRounding.AwayFromZero);
            }

            if (string.Equals(baseCurrencyCode, counterCurrencyCode, StringComparison.OrdinalIgnoreCase) || counterRateToBase <= 0m)
            {
                return decimal.Round(baseAmount, 4, MidpointRounding.AwayFromZero);
            }

            return decimal.Round(baseAmount / counterRateToBase, 4, MidpointRounding.AwayFromZero);
        }

        private static string? NormalizeCurrencyCode(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var normalized = rawValue.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : null;
        }
    }
}
