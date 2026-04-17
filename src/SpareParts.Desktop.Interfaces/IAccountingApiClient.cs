using SpareParts.Domain.Accounting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IAccountingApiClient
    {
        Task<List<AccountTypeDefinitionDto>> GetAccountTypesAsync();
        Task<string> CreateAccountTypeAsync(SaveAccountTypeRequest request);
        Task UpdateAccountTypeAsync(string typeKey, SaveAccountTypeRequest request);
        Task DeleteAccountTypeAsync(string typeKey);
        Task<List<PostingRoleDefinitionDto>> GetPostingRolesAsync();
        Task<string> CreatePostingRoleAsync(SavePostingRoleRequest request);
        Task UpdatePostingRoleAsync(string roleKey, SavePostingRoleRequest request);
        Task DeletePostingRoleAsync(string roleKey);
        Task<List<AccountDto>> GetAccountsAsync();
        Task<int> CreateAccountAsync(CreateAccountRequest request);
        Task UpdateAccountAsync(int id, CreateAccountRequest request);
        Task DeleteAccountAsync(int id);
        Task<List<PostingAccountSettingDto>> GetPostingSettingsAsync();
        Task SavePostingSettingsAsync(UpdatePostingSettingsRequest request);
        Task<List<JournalEntrySummaryDto>> GetJournalEntriesAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<JournalEntryDetailDto> GetJournalEntryAsync(int id);
        Task<int> CreateManualJournalAsync(CreateManualJournalEntryRequest request);
        Task<LedgerReportDto> GetLedgerAsync(int accountId, DateTime? dateFrom = null, DateTime? dateTo = null);
        Task<TrialBalanceReportDto> GetTrialBalanceAsync(DateTime? dateFrom = null, DateTime? dateTo = null);
    }
}
