using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces.Repositories;
using System.Text;

namespace SpareParts.Infrastructure.Services
{
    public sealed class AccountingService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingSettingsProvider _settingsProvider;

        public AccountingService(ISqlConnectionFactory factory, AccountingSettingsProvider settingsProvider)
        {
            _factory = factory;
            _settingsProvider = settingsProvider;
        }

        public IEnumerable<AccountDto> GetAccounts()
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Accounting.Accounts.GetAll().ToList();
        }

        public IEnumerable<AccountTypeDefinitionDto> GetAccountTypes()
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Accounting.Lookups.GetAccountTypes();
        }

        public IEnumerable<PostingRoleDefinitionDto> GetPostingRoles()
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Accounting.Lookups.GetPostingRoles();
        }

        public string CreateAccountType(SaveAccountTypeRequest request)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var normalized = NormalizeAccountTypeRequest(request);

            if (repositories.Lookups.GetAccountType(normalized.TypeKey) != null)
            {
                throw new ConflictException($"Account type '{normalized.TypeKey}' already exists.");
            }

            repositories.Lookups.InsertAccountType(new AccountTypeDefinitionDto
            {
                TypeKey = normalized.TypeKey,
                Label = normalized.Label,
                Description = normalized.Description,
                SortOrder = normalized.SortOrder,
                IsActive = true
            });

            session.Commit();
            return normalized.TypeKey;
        }

        public void UpdateAccountType(string typeKey, SaveAccountTypeRequest request)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var existing = repositories.Lookups.GetAccountType(typeKey) ?? throw new NotFoundException("Account type not found.");
            var normalized = NormalizeAccountTypeRequest(request, typeKey);

            if (!string.Equals(existing.TypeKey, normalized.TypeKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Changing an account type key is not supported.");
            }

            if (!repositories.Lookups.UpdateAccountType(existing.TypeKey, normalized.Label, normalized.Description, normalized.SortOrder))
            {
                throw new NotFoundException("Account type not found.");
            }

            session.Commit();
        }

        public void DeleteAccountType(string typeKey)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            _ = repositories.Lookups.GetAccountType(typeKey) ?? throw new NotFoundException("Account type not found.");

            if (repositories.Lookups.UsesAccountType(typeKey))
            {
                throw new ValidationException($"Cannot delete account type '{typeKey}' because it is used by existing accounts.");
            }

            if (!repositories.Lookups.DeleteAccountType(typeKey))
            {
                throw new NotFoundException("Account type not found.");
            }

            session.Commit();
        }

        public string CreatePostingRole(SavePostingRoleRequest request)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var normalized = NormalizePostingRoleRequest(request);

            if (repositories.Lookups.GetPostingRole(normalized.RoleKey) != null)
            {
                throw new ConflictException($"Posting role '{normalized.RoleKey}' already exists.");
            }

            repositories.Lookups.InsertPostingRole(new PostingRoleDefinitionDto
            {
                RoleKey = normalized.RoleKey,
                Label = normalized.Label,
                Description = normalized.Description,
                SortOrder = normalized.SortOrder,
                IsActive = true
            });

            session.Commit();
            return normalized.RoleKey;
        }

        public void UpdatePostingRole(string roleKey, SavePostingRoleRequest request)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var existing = repositories.Lookups.GetPostingRole(roleKey) ?? throw new NotFoundException("Posting role not found.");
            var normalized = NormalizePostingRoleRequest(request, roleKey);

            if (!string.Equals(existing.RoleKey, normalized.RoleKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Changing a posting role key is not supported.");
            }

            if (!repositories.Lookups.UpdatePostingRole(existing.RoleKey, normalized.Label, normalized.Description, normalized.SortOrder))
            {
                throw new NotFoundException("Posting role not found.");
            }

            session.Commit();
        }

        public void DeletePostingRole(string roleKey)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            _ = repositories.Lookups.GetPostingRole(roleKey) ?? throw new NotFoundException("Posting role not found.");

            if (AccountingSettingKeys.All.Contains(roleKey, StringComparer.OrdinalIgnoreCase))
            {
                throw new ValidationException($"Cannot delete core posting role '{roleKey}'.");
            }

            if (repositories.Lookups.UsesPostingRole(roleKey))
            {
                repositories.PostingSettings.Delete(roleKey);
            }

            if (!repositories.Lookups.DeletePostingRole(roleKey))
            {
                throw new NotFoundException("Posting role not found.");
            }

            session.Commit();
        }

        public int CreateAccount(CreateAccountRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var accountTypes = repositories.Lookups.GetAccountTypes();
            var normalized = NormalizeAccountRequest(request, accountTypes);

            ValidateAccount(normalized, repositories.Accounts, accountTypes);

            var account = new Account
            {
                Code = normalized.Code,
                Name = normalized.Name,
                AccountTypeKey = normalized.AccountTypeKey,
                ParentId = normalized.ParentId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var id = repositories.Accounts.Insert(account);
            session.Commit();
            return id;
        }

        public void UpdateAccount(int id, CreateAccountRequest request, int userId)
        {
            if (id <= 0)
            {
                throw new ValidationException("Invalid account id.");
            }

            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var accountTypes = repositories.Lookups.GetAccountTypes();
            var normalized = NormalizeAccountRequest(request, accountTypes);
            _ = repositories.Accounts.GetById(id) ?? throw new NotFoundException("Account not found.");

            ValidateAccount(normalized, repositories.Accounts, accountTypes, id);

            if (normalized.ParentId == id)
            {
                throw new ValidationException("An account cannot be its own parent.");
            }

            if (!repositories.Accounts.Update(id, normalized.Code, normalized.Name, normalized.AccountTypeKey, normalized.ParentId, userId))
            {
                throw new NotFoundException("Account not found.");
            }

            session.Commit();
        }

        public void DeleteAccount(int id)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var account = repositories.Accounts.GetById(id) ?? throw new NotFoundException("Account not found.");

            if (repositories.Accounts.HasChildren(id))
            {
                throw new ValidationException($"Cannot delete account '{account.Code}' because it has child accounts.");
            }

            if (repositories.PostingSettings.UsesAccount(id))
            {
                throw new ValidationException($"Cannot delete account '{account.Code}' because it is used in posting setup.");
            }

            if (repositories.Journal.HasEntriesForAccount(id))
            {
                throw new ValidationException($"Cannot delete account '{account.Code}' because it already has journal activity.");
            }

            if (new CustomersRepository(session).UsesAccount(id))
            {
                throw new ValidationException($"Cannot delete account '{account.Code}' because it is linked to a customer.");
            }

            if (new SuppliersRepository(session).UsesAccount(id))
            {
                throw new ValidationException($"Cannot delete account '{account.Code}' because it is linked to a supplier.");
            }

            if (!repositories.Accounts.Delete(id))
            {
                throw new NotFoundException("Account not found.");
            }

            session.Commit();
        }

        public IEnumerable<PostingAccountSettingDto> GetPostingSettings()
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var accounts = repositories.Accounts.GetAll().ToDictionary(account => account.Id);
            var rows = repositories.PostingSettings.GetAll().ToDictionary(item => item.SettingKey, item => item.AccountId, StringComparer.OrdinalIgnoreCase);
            var defaults = _settingsProvider.GetSnapshot();
            var roleDefinitions = repositories.Lookups.GetPostingRoles();

            return roleDefinitions.Select(definition =>
            {
                var accountId = rows.TryGetValue(definition.RoleKey, out var configuredId)
                    ? configuredId
                    : GetFallbackAccountId(defaults, definition.RoleKey);

                var isConfigured = rows.ContainsKey(definition.RoleKey);

                if (accountId is not > 0)
                {
                    return new PostingAccountSettingDto
                    {
                        SettingKey = definition.RoleKey,
                        Label = definition.Label,
                        Description = definition.Description,
                        AccountId = null,
                        IsConfigured = false
                    };
                }

                if (!accounts.TryGetValue(accountId.Value, out var account))
                {
                    throw new ValidationException($"Posting setup references missing account id {accountId}.");
                }

                return new PostingAccountSettingDto
                {
                    SettingKey = definition.RoleKey,
                    Label = definition.Label,
                    Description = definition.Description,
                    AccountId = account.Id,
                    AccountCode = account.Code,
                    AccountName = account.Name,
                    AccountTypeKey = account.AccountTypeKey,
                    AccountType = account.AccountType,
                    IsConfigured = isConfigured
                };
            }).ToList();
        }

        public void UpdatePostingSettings(UpdatePostingSettingsRequest request, int userId)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                throw new ValidationException("Posting settings are required.");
            }

            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var accounts = repositories.Accounts.GetAll().ToDictionary(account => account.Id);
            var validRoleKeys = repositories.Lookups.GetPostingRoles()
                .Select(role => role.RoleKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var providedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in request.Items)
            {
                var key = (item.SettingKey ?? string.Empty).Trim();
                if (!validRoleKeys.Contains(key))
                {
                    throw new ValidationException($"Unsupported posting setting '{item.SettingKey}'.");
                }

                if (!providedKeys.Add(key))
                {
                    throw new ValidationException($"Duplicate posting setting '{item.SettingKey}'.");
                }

                if (item.AccountId is > 0 && !accounts.ContainsKey(item.AccountId.Value))
                {
                    throw new ValidationException($"Account id {item.AccountId} does not exist.");
                }

                if (item.AccountId is > 0)
                {
                    repositories.PostingSettings.Upsert(key, item.AccountId.Value, userId);
                }
                else
                {
                    repositories.PostingSettings.Delete(key);
                }
            }

            session.Commit();
        }

        public IEnumerable<JournalEntrySummaryDto> GetJournalEntries(DateTime? dateFrom, DateTime? dateTo)
        {
            ValidateDateRange(dateFrom, dateTo);

            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Accounting.Journal.GetEntries(dateFrom, dateTo);
        }

        public JournalEntryDetailDto GetJournalEntry(int id)
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Accounting.Journal.GetEntryDetail(id) ?? throw new NotFoundException("Journal entry not found.");
        }

        public int CreateManualJournal(CreateManualJournalEntryRequest request, int userId)
        {
            if (request == null)
            {
                throw new ValidationException("Journal request is required.");
            }

            if (request.Lines == null || request.Lines.Count < 2)
            {
                throw new ValidationException("At least two journal lines are required.");
            }

            var normalizedDescription = (request.Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedDescription))
            {
                throw new ValidationException("Journal description is required.");
            }

            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var accounts = repositories.Accounts.GetAll().ToDictionary(account => account.Id);

            var lines = request.Lines.Select(line =>
            {
                if (!accounts.ContainsKey(line.AccountId))
                {
                    throw new ValidationException($"Account id {line.AccountId} does not exist.");
                }

                if (line.Debit < 0 || line.Credit < 0)
                {
                    throw new ValidationException("Journal line amounts cannot be negative.");
                }

                if ((line.Debit > 0m ? 1 : 0) + (line.Credit > 0m ? 1 : 0) != 1)
                {
                    throw new ValidationException("Each journal line must contain either a debit or a credit.");
                }

                return new JournalLine
                {
                    AccountId = line.AccountId,
                    Debit = decimal.Round(line.Debit, 4, MidpointRounding.AwayFromZero),
                    Credit = decimal.Round(line.Credit, 4, MidpointRounding.AwayFromZero),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
            }).ToList();

            var totalDebit = decimal.Round(lines.Sum(line => line.Debit), 4, MidpointRounding.AwayFromZero);
            var totalCredit = decimal.Round(lines.Sum(line => line.Credit), 4, MidpointRounding.AwayFromZero);
            if (totalDebit != totalCredit)
            {
                throw new ValidationException("Journal entry is not balanced.");
            }

            var entry = new JournalEntry
            {
                EntryDate = request.EntryDate,
                ReferenceType = string.IsNullOrWhiteSpace(request.ReferenceType) ? "Manual" : request.ReferenceType.Trim(),
                ReferenceId = request.ReferenceId,
                Description = normalizedDescription,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = repositories.Journal.InsertEntry(entry);
            repositories.Journal.InsertLines(entryId, lines);
            session.Commit();
            return entryId;
        }

        public LedgerReportDto GetLedger(int accountId, DateTime? dateFrom, DateTime? dateTo)
        {
            if (accountId <= 0)
            {
                throw new ValidationException("Account id is required.");
            }

            ValidateDateRange(dateFrom, dateTo);

            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session).Accounting;
            var account = repositories.Accounts.GetAll().FirstOrDefault(item => item.Id == accountId)
                ?? throw new NotFoundException("Account not found.");
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            var openingBalance = repositories.Journal.GetOpeningBalance(accountId, dateFrom);
            var openingCounterBalance = repositories.Journal.GetOpeningCounterBalance(accountId, dateFrom);
            var rows = repositories.Journal.GetLedgerRows(accountId, dateFrom, dateTo).ToList();
            var runningBalance = openingBalance;
            var runningCounterBalance = openingCounterBalance;

            foreach (var row in rows)
            {
                runningBalance += row.Debit - row.Credit;
                runningCounterBalance += row.CounterDebit - row.CounterCredit;
                row.RunningBalance = runningBalance;
                row.RunningCounterBalance = runningCounterBalance;
            }

            return new LedgerReportDto
            {
                AccountId = account.Id,
                AccountCode = account.Code,
                AccountName = account.Name,
                BaseCurrencyCode = rows.FirstOrDefault()?.BaseCurrencyCode ?? currencyContext.BaseCurrencyCode,
                CounterCurrencyCode = rows.FirstOrDefault()?.CounterCurrencyCode ?? currencyContext.CounterCurrencyCode,
                DateFrom = dateFrom,
                DateTo = dateTo,
                OpeningBalance = openingBalance,
                OpeningCounterBalance = openingCounterBalance,
                ClosingBalance = runningBalance,
                ClosingCounterBalance = runningCounterBalance,
                Entries = rows
            };
        }

        public TrialBalanceReportDto GetTrialBalance(DateTime? dateFrom, DateTime? dateTo)
        {
            ValidateDateRange(dateFrom, dateTo);

            using var session = new DbSession(_factory);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);
            var rows = RepositoryCatalog.For(session).Accounting.Journal.GetTrialBalanceRows(dateFrom, dateTo).ToList();

            return new TrialBalanceReportDto
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                BaseCurrencyCode = rows.FirstOrDefault()?.BaseCurrencyCode ?? currencyContext.BaseCurrencyCode,
                CounterCurrencyCode = rows.FirstOrDefault()?.CounterCurrencyCode ?? currencyContext.CounterCurrencyCode,
                TotalDebit = rows.Sum(row => row.TotalDebit),
                TotalCredit = rows.Sum(row => row.TotalCredit),
                TotalCounterDebit = rows.Sum(row => row.TotalCounterDebit),
                TotalCounterCredit = rows.Sum(row => row.TotalCounterCredit),
                Rows = rows
            };
        }

        public StatementOfAccountReportDto GetStatementOfAccount(int accountId, DateTime? dateFrom, DateTime? dateTo)
        {
            var ledger = GetLedger(accountId, dateFrom, dateTo);

            return new StatementOfAccountReportDto
            {
                AccountId = ledger.AccountId,
                AccountCode = ledger.AccountCode,
                AccountName = ledger.AccountName,
                BaseCurrencyCode = ledger.BaseCurrencyCode,
                CounterCurrencyCode = ledger.CounterCurrencyCode,
                DateFrom = ledger.DateFrom,
                DateTo = ledger.DateTo,
                OpeningBalance = ledger.OpeningBalance,
                ClosingBalance = ledger.ClosingBalance,
                OpeningCounterBalance = ledger.OpeningCounterBalance,
                ClosingCounterBalance = ledger.ClosingCounterBalance,
                TotalDebit = ledger.Entries.Sum(entry => entry.Debit),
                TotalCredit = ledger.Entries.Sum(entry => entry.Credit),
                TotalCounterDebit = ledger.Entries.Sum(entry => entry.CounterDebit),
                TotalCounterCredit = ledger.Entries.Sum(entry => entry.CounterCredit),
                Entries = ledger.Entries.Select(entry => new StatementOfAccountRowDto
                {
                    JournalEntryId = entry.JournalEntryId,
                    EntryDate = entry.EntryDate,
                    ReferenceType = entry.ReferenceType,
                    ReferenceId = entry.ReferenceId,
                    Description = entry.Description,
                    BaseCurrencyCode = entry.BaseCurrencyCode,
                    CounterCurrencyCode = entry.CounterCurrencyCode,
                    CurrencyCode = entry.CurrencyCode,
                    OriginalAmount = entry.OriginalAmount,
                    Debit = entry.Debit,
                    Credit = entry.Credit,
                    CounterDebit = entry.CounterDebit,
                    CounterCredit = entry.CounterCredit,
                    RunningBalance = entry.RunningBalance,
                    RunningCounterBalance = entry.RunningCounterBalance
                }).ToList()
            };
        }

        private static (string Code, string Name, string AccountTypeKey, int? ParentId) NormalizeAccountRequest(
            CreateAccountRequest request,
            IReadOnlyCollection<AccountTypeDefinitionDto> accountTypes)
        {
            if (request == null)
            {
                throw new ValidationException("Account request is required.");
            }

            var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ValidationException("Account code is required.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ValidationException("Account name is required.");
            }

            var accountTypeKey = (request.AccountTypeKey ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(accountTypeKey))
            {
                accountTypeKey = accountTypes.OrderBy(type => type.SortOrder).Select(type => type.TypeKey).FirstOrDefault() ?? "asset";
            }

            return (code, name, accountTypeKey, request.ParentId);
        }

        private static void ValidateAccount(
            (string Code, string Name, string AccountTypeKey, int? ParentId) normalized,
            IAccountsRepository accountsRepository,
            IReadOnlyCollection<AccountTypeDefinitionDto> accountTypes,
            int? existingId = null)
        {
            var existingByCode = accountsRepository.GetByCode(normalized.Code);
            if (existingByCode != null && existingByCode.Id != existingId)
            {
                throw new ConflictException($"Account code '{normalized.Code}' already exists.");
            }

            if (!accountTypes.Any(type => string.Equals(type.TypeKey, normalized.AccountTypeKey, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ValidationException($"Unsupported account type '{normalized.AccountTypeKey}'.");
            }

            if (normalized.ParentId is int parentId)
            {
                if (parentId <= 0)
                {
                    throw new ValidationException("Parent account id is invalid.");
                }

                var parent = accountsRepository.GetById(parentId);
                if (parent == null)
                {
                    throw new ValidationException("Selected parent account does not exist.");
                }
            }
        }

        private static void ValidateDateRange(DateTime? dateFrom, DateTime? dateTo)
        {
            if (dateFrom != null && dateTo != null && dateFrom.Value.Date > dateTo.Value.Date)
            {
                throw new ValidationException("Date from cannot be after date to.");
            }
        }

        private static int? GetFallbackAccountId(AccountingPostingSettingsSnapshot defaults, string key)
            => key switch
            {
                AccountingSettingKeys.SalesCash => defaults.SalesCashAccountId,
                AccountingSettingKeys.SalesRevenue => defaults.SalesRevenueAccountId,
                AccountingSettingKeys.Cogs => defaults.CogsAccountId,
                AccountingSettingKeys.Inventory => defaults.InventoryAccountId,
                AccountingSettingKeys.PurchaseOffset => defaults.PurchaseOffsetAccountId,
                _ => null
            };

        private static (string TypeKey, string Label, string Description, int SortOrder) NormalizeAccountTypeRequest(
            SaveAccountTypeRequest request,
            string? existingKey = null)
        {
            if (request == null)
            {
                throw new ValidationException("Account type request is required.");
            }

            var typeKey = NormalizeLookupKey(existingKey ?? request.TypeKey, "Account type key");
            var label = (request.Label ?? string.Empty).Trim();
            var description = (request.Description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ValidationException("Account type label is required.");
            }

            return (typeKey, label, description, request.SortOrder);
        }

        private static (string RoleKey, string Label, string Description, int SortOrder) NormalizePostingRoleRequest(
            SavePostingRoleRequest request,
            string? existingKey = null)
        {
            if (request == null)
            {
                throw new ValidationException("Posting role request is required.");
            }

            var roleKey = NormalizeLookupKey(existingKey ?? request.RoleKey, "Posting role key");
            var label = (request.Label ?? string.Empty).Trim();
            var description = (request.Description ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ValidationException("Posting role label is required.");
            }

            return (roleKey, label, description, request.SortOrder);
        }

        private static string NormalizeLookupKey(string rawKey, string fieldName)
        {
            var input = (rawKey ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ValidationException($"{fieldName} is required.");
            }

            var builder = new StringBuilder(input.Length);
            var lastWasSeparator = false;

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasSeparator = false;
                    continue;
                }

                if ((ch == '_' || ch == '-' || char.IsWhiteSpace(ch)) && builder.Length > 0 && !lastWasSeparator)
                {
                    builder.Append('_');
                    lastWasSeparator = true;
                }
            }

            var normalized = builder.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ValidationException($"{fieldName} is invalid.");
            }

            return normalized;
        }
    }
}
