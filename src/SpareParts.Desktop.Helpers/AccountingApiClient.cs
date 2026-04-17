using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Accounting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class AccountingApiClient : FeatureApiClientBase, IAccountingApiClient
    {
        public AccountingApiClient(IRestClientFactory restClientFactory, IApiTokenProvider tokenProvider)
            : base(restClientFactory, tokenProvider, AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<AccountTypeDefinitionDto>> GetAccountTypesAsync()
            => RetrieveAsync<AccountTypeDefinitionDto>("api/accounting/account-types");

        public Task<string> CreateAccountTypeAsync(SaveAccountTypeRequest request)
            => AddAsync<string>("api/accounting/account-types", request, "Account type creation did not return a key.");

        public Task UpdateAccountTypeAsync(string typeKey, SaveAccountTypeRequest request)
            => EditAsync($"api/accounting/account-types/{Uri.EscapeDataString(typeKey)}", request);

        public Task DeleteAccountTypeAsync(string typeKey)
            => DeleteResourceAsync($"api/accounting/account-types/{Uri.EscapeDataString(typeKey)}");

        public Task<List<PostingRoleDefinitionDto>> GetPostingRolesAsync()
            => RetrieveAsync<PostingRoleDefinitionDto>("api/accounting/posting-roles");

        public Task<string> CreatePostingRoleAsync(SavePostingRoleRequest request)
            => AddAsync<string>("api/accounting/posting-roles", request, "Posting role creation did not return a key.");

        public Task UpdatePostingRoleAsync(string roleKey, SavePostingRoleRequest request)
            => EditAsync($"api/accounting/posting-roles/{Uri.EscapeDataString(roleKey)}", request);

        public Task DeletePostingRoleAsync(string roleKey)
            => DeleteResourceAsync($"api/accounting/posting-roles/{Uri.EscapeDataString(roleKey)}");

        public Task<List<AccountDto>> GetAccountsAsync()
            => RetrieveAsync<AccountDto>("api/accounts");

        public Task<int> CreateAccountAsync(CreateAccountRequest request)
            => AddAsync<int>("api/accounts", request, "Account creation did not return an id.");

        public Task UpdateAccountAsync(int id, CreateAccountRequest request)
            => EditAsync($"api/accounts/{id}", request);

        public Task DeleteAccountAsync(int id)
            => DeleteResourceAsync($"api/accounts/{id}");

        public Task<List<PostingAccountSettingDto>> GetPostingSettingsAsync()
            => RetrieveAsync<PostingAccountSettingDto>("api/accounting/posting-settings");

        public Task SavePostingSettingsAsync(UpdatePostingSettingsRequest request)
            => EditAsync("api/accounting/posting-settings", request);

        public Task<List<JournalEntrySummaryDto>> GetJournalEntriesAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
            => RetrieveAsync<JournalEntrySummaryDto>($"api/accounting/journal-entries{BuildDateQuery(dateFrom, dateTo)}");

        public Task<JournalEntryDetailDto> GetJournalEntryAsync(int id)
            => RetrieveOneAsync<JournalEntryDetailDto>($"api/accounting/journal-entries/{id}", "Journal entry not found.");

        public Task<int> CreateManualJournalAsync(CreateManualJournalEntryRequest request)
            => AddAsync<int>("api/accounting/journal-entries/manual", request, "Manual journal did not return an id.");

        public Task<LedgerReportDto> GetLedgerAsync(int accountId, DateTime? dateFrom = null, DateTime? dateTo = null)
            => RetrieveOneAsync<LedgerReportDto>($"api/accounting/ledger?accountId={accountId}{BuildDateQuery(dateFrom, dateTo, hasExistingQuery: true)}", "Ledger report not found.");

        public Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime? dateFrom = null, DateTime? dateTo = null)
            => RetrieveOneAsync<TrialBalanceReportDto>($"api/accounting/trial-balance{BuildDateQuery(dateFrom, dateTo)}", "Trial balance report was empty.");

        private static string BuildDateQuery(DateTime? dateFrom, DateTime? dateTo, bool hasExistingQuery = false)
        {
            var builder = new StringBuilder();
            var hasAny = hasExistingQuery;

            if (dateFrom != null)
            {
                builder.Append(hasAny ? '&' : '?');
                builder.Append("dateFrom=");
                builder.Append(Uri.EscapeDataString(dateFrom.Value.ToString("yyyy-MM-dd")));
                hasAny = true;
            }

            if (dateTo != null)
            {
                builder.Append(hasAny ? '&' : '?');
                builder.Append("dateTo=");
                builder.Append(Uri.EscapeDataString(dateTo.Value.ToString("yyyy-MM-dd")));
            }

            return builder.ToString();
        }
    }
}
