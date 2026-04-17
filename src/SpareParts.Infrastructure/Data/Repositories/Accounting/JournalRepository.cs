using Dapper;
using SpareParts.Domain.Accounting;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class JournalRepository : IJournalRepository
    {
        private readonly DbSession _session;

        public JournalRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertEntry(JournalEntry entry)
        {
            const string sql = @"INSERT INTO JournalEntries
                (EntryDate, ReferenceType, ReferenceId, Description, CreatedAt, CreatedByUserId)
                VALUES
                (@EntryDate, @ReferenceType, @ReferenceId, @Description, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, entry, _session.Transaction);
        }

        public void InsertLines(int entryId, IList<JournalLine> lines)
        {
            const string sql = @"INSERT INTO JournalLines
                (JournalEntryId, AccountId, Debit, Credit, CreatedAt, CreatedByUserId)
                VALUES
                (@JournalEntryId, @AccountId, @Debit, @Credit, @CreatedAt, @CreatedByUserId);";
            foreach (var line in lines)
            {
                line.JournalEntryId = entryId;
                _session.Connection.Execute(sql, line, _session.Transaction);
            }
        }

        public void DeleteEntriesByReference(string referenceType, int referenceId)
        {
            const string deleteLinesSql = @"DELETE jl
                FROM JournalLines jl
                INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                WHERE je.ReferenceType = @ReferenceType AND je.ReferenceId = @ReferenceId;";

            const string deleteEntriesSql = @"DELETE FROM JournalEntries
                WHERE ReferenceType = @ReferenceType AND ReferenceId = @ReferenceId;";

            var args = new { ReferenceType = referenceType, ReferenceId = referenceId };
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
                                 GROUP BY je.Id, je.EntryDate, je.ReferenceType, je.ReferenceId, je.Description, je.CreatedAt, je.CreatedByUserId
                                 ORDER BY je.EntryDate DESC, je.Id DESC;";

            return _session.Connection.Query<JournalEntrySummaryDto>(sql, new { DateFrom = dateFrom, DateTo = dateTo }, _session.Transaction).ToList();
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
                                       GROUP BY je.Id, je.EntryDate, je.ReferenceType, je.ReferenceId, je.Description, je.CreatedAt, je.CreatedByUserId;";

            const string linesSql = @"SELECT jl.AccountId,
                                             a.Code AS AccountCode,
                                             a.Name AS AccountName,
                                             jl.Debit,
                                             jl.Credit
                                      FROM JournalLines jl
                                      INNER JOIN Accounts a ON a.Id = jl.AccountId
                                      WHERE jl.JournalEntryId = @Id
                                      ORDER BY a.Code, a.Name;";

            var detail = _session.Connection.QueryFirstOrDefault<JournalEntryDetailDto>(headerSql, new { Id = id }, _session.Transaction);
            if (detail == null)
            {
                return null;
            }

            detail.Lines = _session.Connection.Query<JournalEntryLineDto>(linesSql, new { Id = id }, _session.Transaction).ToList();
            return detail;
        }

        public IReadOnlyList<LedgerRowDto> GetLedgerRows(int accountId, DateTime? dateFrom, DateTime? dateTo)
        {
            const string sql = @"SELECT je.Id AS JournalEntryId,
                                        je.EntryDate,
                                        je.ReferenceType,
                                        je.ReferenceId,
                                        je.Description,
                                        jl.Debit,
                                        jl.Credit,
                                        CAST(0 AS DECIMAL(19,4)) AS RunningBalance
                                 FROM JournalLines jl
                                 INNER JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                                 WHERE jl.AccountId = @AccountId
                                   AND (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                   AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                 ORDER BY je.EntryDate, je.Id, jl.Id;";

            return _session.Connection.Query<LedgerRowDto>(sql, new
            {
                AccountId = accountId,
                DateFrom = dateFrom,
                DateTo = dateTo
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
                                   AND je.EntryDate < @DateFrom;";

            return _session.Connection.ExecuteScalar<decimal>(sql, new { AccountId = accountId, DateFrom = dateFrom }, _session.Transaction);
        }

        public IReadOnlyList<TrialBalanceRowDto> GetTrialBalanceRows(DateTime? dateFrom, DateTime? dateTo)
        {
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
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                        THEN jl.Debit ELSE 0 END), 0) AS TotalDebit,
                                    ISNULL(SUM(CASE
                                        WHEN (@DateFrom IS NULL OR je.EntryDate >= @DateFrom)
                                         AND (@DateTo IS NULL OR je.EntryDate < DATEADD(DAY, 1, @DateTo))
                                        THEN jl.Credit ELSE 0 END), 0) AS TotalCredit
                             FROM Accounts a
                             {(hasAccountTypeLookup ? $"LEFT JOIN AccountingAccountTypes t ON t.TypeKey = {normalizedAccountTypeKey}" : string.Empty)}
                             LEFT JOIN JournalLines jl ON jl.AccountId = a.Id
                             LEFT JOIN JournalEntries je ON je.Id = jl.JournalEntryId
                             GROUP BY a.Id, a.Code, a.Name, {normalizedAccountTypeKey}{(hasAccountTypeLookup ? ", t.Label" : string.Empty)}
                         )
                         SELECT AccountId,
                                AccountCode,
                                AccountName,
                                AccountTypeKey,
                                AccountType,
                                TotalDebit,
                                TotalCredit,
                                CASE WHEN TotalDebit >= TotalCredit THEN TotalDebit - TotalCredit ELSE 0 END AS DebitBalance,
                                CASE WHEN TotalCredit > TotalDebit THEN TotalCredit - TotalDebit ELSE 0 END AS CreditBalance
                         FROM AccountTotals
                         WHERE TotalDebit <> 0 OR TotalCredit <> 0
                         ORDER BY AccountCode, AccountName;";

            return _session.Connection.Query<TrialBalanceRowDto>(sql, new { DateFrom = dateFrom, DateTo = dateTo }, _session.Transaction).ToList();
        }

        public bool HasEntriesForAccount(int accountId)
        {
            const string sql = "SELECT COUNT(1) FROM JournalLines WHERE AccountId = @AccountId;";
            return _session.Connection.ExecuteScalar<int>(sql, new { AccountId = accountId }, _session.Transaction) > 0;
        }
    }
}
