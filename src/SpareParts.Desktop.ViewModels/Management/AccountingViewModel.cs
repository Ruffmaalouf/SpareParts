using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Accounting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class AccountingViewModel : INotifyPropertyChanged
    {
        private enum AccountingLoadScope
        {
            Setup,
            Review,
            ManualJournal
        }

        private readonly IAccountingApiClient _accountingApi;
        private bool _suppressJournalSelectionLoad;
        private bool _suppressLedgerRefresh;

        public ObservableCollection<AccountDto> Accounts { get; } = new();
        public ObservableCollection<AccountTypeDefinitionDto> AccountTypes { get; } = new();
        public ObservableCollection<AccountSelectorOption> ParentAccountOptions { get; } = new();
        public ObservableCollection<AccountSelectorOption> PostingAccountOptions { get; } = new();
        public ObservableCollection<PostingSettingEditorRow> PostingSettings { get; } = new();
        public ObservableCollection<PostingRoleDefinitionDto> PostingRoleDefinitions { get; } = new();
        public ObservableCollection<JournalEntrySummaryDto> JournalEntries { get; } = new();
        public ObservableCollection<JournalEntryLineDto> SelectedJournalLines { get; } = new();
        public ObservableCollection<ManualJournalLineEditor> ManualJournalLines { get; } = new();
        public ObservableCollection<LedgerRowDto> LedgerEntries { get; } = new();
        public ObservableCollection<TrialBalanceRowDto> TrialBalanceRows { get; } = new();

        private AccountDto? _selectedAccount;
        public AccountDto? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                if (_selectedAccount == value) return;
                _selectedAccount = value;
                OnPropertyChanged(nameof(SelectedAccount));
                PopulateAccountForm(value);
            }
        }

        private string _accountCode = string.Empty;
        public string AccountCode
        {
            get => _accountCode;
            set { _accountCode = value; OnPropertyChanged(nameof(AccountCode)); }
        }

        private string _accountName = string.Empty;
        public string AccountName
        {
            get => _accountName;
            set { _accountName = value; OnPropertyChanged(nameof(AccountName)); }
        }

        private string _selectedAccountTypeKey = "asset";
        public string SelectedAccountTypeKey
        {
            get => _selectedAccountTypeKey;
            set { _selectedAccountTypeKey = value; OnPropertyChanged(nameof(SelectedAccountTypeKey)); }
        }

        private int? _selectedParentAccountId;
        public int? SelectedParentAccountId
        {
            get => _selectedParentAccountId;
            set { _selectedParentAccountId = value; OnPropertyChanged(nameof(SelectedParentAccountId)); }
        }

        private AccountTypeDefinitionDto? _selectedAccountTypeDefinition;
        public AccountTypeDefinitionDto? SelectedAccountTypeDefinition
        {
            get => _selectedAccountTypeDefinition;
            set
            {
                if (_selectedAccountTypeDefinition == value) return;
                _selectedAccountTypeDefinition = value;
                OnPropertyChanged(nameof(SelectedAccountTypeDefinition));
                OnPropertyChanged(nameof(IsEditingAccountTypeDefinition));
                PopulateAccountTypeDefinitionForm(value);
            }
        }

        public bool IsEditingAccountTypeDefinition => SelectedAccountTypeDefinition != null;

        private string _accountTypeDefinitionKey = string.Empty;
        public string AccountTypeDefinitionKey
        {
            get => _accountTypeDefinitionKey;
            set { _accountTypeDefinitionKey = value; OnPropertyChanged(nameof(AccountTypeDefinitionKey)); }
        }

        private string _accountTypeDefinitionLabel = string.Empty;
        public string AccountTypeDefinitionLabel
        {
            get => _accountTypeDefinitionLabel;
            set { _accountTypeDefinitionLabel = value; OnPropertyChanged(nameof(AccountTypeDefinitionLabel)); }
        }

        private string _accountTypeDefinitionDescription = string.Empty;
        public string AccountTypeDefinitionDescription
        {
            get => _accountTypeDefinitionDescription;
            set { _accountTypeDefinitionDescription = value; OnPropertyChanged(nameof(AccountTypeDefinitionDescription)); }
        }

        private int _accountTypeDefinitionSortOrder = 10;
        public int AccountTypeDefinitionSortOrder
        {
            get => _accountTypeDefinitionSortOrder;
            set { _accountTypeDefinitionSortOrder = value; OnPropertyChanged(nameof(AccountTypeDefinitionSortOrder)); }
        }

        private PostingRoleDefinitionDto? _selectedPostingRoleDefinition;
        public PostingRoleDefinitionDto? SelectedPostingRoleDefinition
        {
            get => _selectedPostingRoleDefinition;
            set
            {
                if (_selectedPostingRoleDefinition == value) return;
                _selectedPostingRoleDefinition = value;
                OnPropertyChanged(nameof(SelectedPostingRoleDefinition));
                OnPropertyChanged(nameof(IsEditingPostingRoleDefinition));
                PopulatePostingRoleDefinitionForm(value);
            }
        }

        public bool IsEditingPostingRoleDefinition => SelectedPostingRoleDefinition != null;

        private string _postingRoleDefinitionKey = string.Empty;
        public string PostingRoleDefinitionKey
        {
            get => _postingRoleDefinitionKey;
            set { _postingRoleDefinitionKey = value; OnPropertyChanged(nameof(PostingRoleDefinitionKey)); }
        }

        private string _postingRoleDefinitionLabel = string.Empty;
        public string PostingRoleDefinitionLabel
        {
            get => _postingRoleDefinitionLabel;
            set { _postingRoleDefinitionLabel = value; OnPropertyChanged(nameof(PostingRoleDefinitionLabel)); }
        }

        private string _postingRoleDefinitionDescription = string.Empty;
        public string PostingRoleDefinitionDescription
        {
            get => _postingRoleDefinitionDescription;
            set { _postingRoleDefinitionDescription = value; OnPropertyChanged(nameof(PostingRoleDefinitionDescription)); }
        }

        private int _postingRoleDefinitionSortOrder = 10;
        public int PostingRoleDefinitionSortOrder
        {
            get => _postingRoleDefinitionSortOrder;
            set { _postingRoleDefinitionSortOrder = value; OnPropertyChanged(nameof(PostingRoleDefinitionSortOrder)); }
        }

        private JournalEntrySummaryDto? _selectedJournalEntry;
        public JournalEntrySummaryDto? SelectedJournalEntry
        {
            get => _selectedJournalEntry;
            set
            {
                if (_selectedJournalEntry == value) return;
                _selectedJournalEntry = value;
                OnPropertyChanged(nameof(SelectedJournalEntry));
                if (!_suppressJournalSelectionLoad)
                {
                    _ = LoadSelectedJournalDetailAsync();
                }
            }
        }

        private JournalEntryDetailDto? _selectedJournalDetail;
        public JournalEntryDetailDto? SelectedJournalDetail
        {
            get => _selectedJournalDetail;
            private set
            {
                _selectedJournalDetail = value;
                OnPropertyChanged(nameof(SelectedJournalDetail));
                OnPropertyChanged(nameof(SelectedJournalDetailSummary));
            }
        }

        private ManualJournalLineEditor? _selectedManualJournalLine;
        public ManualJournalLineEditor? SelectedManualJournalLine
        {
            get => _selectedManualJournalLine;
            set { _selectedManualJournalLine = value; OnPropertyChanged(nameof(SelectedManualJournalLine)); }
        }

        private DateTime _manualJournalEntryDate = DateTime.Today;
        public DateTime ManualJournalEntryDate
        {
            get => _manualJournalEntryDate;
            set { _manualJournalEntryDate = value; OnPropertyChanged(nameof(ManualJournalEntryDate)); }
        }

        private string _manualJournalDescription = string.Empty;
        public string ManualJournalDescription
        {
            get => _manualJournalDescription;
            set { _manualJournalDescription = value; OnPropertyChanged(nameof(ManualJournalDescription)); }
        }

        private string _manualJournalReferenceType = "Manual";
        public string ManualJournalReferenceType
        {
            get => _manualJournalReferenceType;
            set { _manualJournalReferenceType = value; OnPropertyChanged(nameof(ManualJournalReferenceType)); }
        }

        private DateTime? _journalDateFrom;
        public DateTime? JournalDateFrom
        {
            get => _journalDateFrom;
            set { _journalDateFrom = value; OnPropertyChanged(nameof(JournalDateFrom)); }
        }

        private DateTime? _journalDateTo;
        public DateTime? JournalDateTo
        {
            get => _journalDateTo;
            set { _journalDateTo = value; OnPropertyChanged(nameof(JournalDateTo)); }
        }

        private int? _selectedLedgerAccountId;
        public int? SelectedLedgerAccountId
        {
            get => _selectedLedgerAccountId;
            set
            {
                if (_selectedLedgerAccountId == value) return;
                _selectedLedgerAccountId = value;
                OnPropertyChanged(nameof(SelectedLedgerAccountId));
                if (!_suppressLedgerRefresh && value is > 0)
                {
                    _ = RefreshLedgerAsync();
                }
            }
        }

        private DateTime? _ledgerDateFrom;
        public DateTime? LedgerDateFrom
        {
            get => _ledgerDateFrom;
            set { _ledgerDateFrom = value; OnPropertyChanged(nameof(LedgerDateFrom)); }
        }

        private DateTime? _ledgerDateTo;
        public DateTime? LedgerDateTo
        {
            get => _ledgerDateTo;
            set { _ledgerDateTo = value; OnPropertyChanged(nameof(LedgerDateTo)); }
        }

        private LedgerReportDto? _ledgerReport;
        public LedgerReportDto? LedgerReport
        {
            get => _ledgerReport;
            private set
            {
                _ledgerReport = value;
                OnPropertyChanged(nameof(LedgerReport));
                OnPropertyChanged(nameof(LedgerSummary));
            }
        }

        private DateTime? _trialBalanceDateFrom;
        public DateTime? TrialBalanceDateFrom
        {
            get => _trialBalanceDateFrom;
            set { _trialBalanceDateFrom = value; OnPropertyChanged(nameof(TrialBalanceDateFrom)); }
        }

        private DateTime? _trialBalanceDateTo;
        public DateTime? TrialBalanceDateTo
        {
            get => _trialBalanceDateTo;
            set { _trialBalanceDateTo = value; OnPropertyChanged(nameof(TrialBalanceDateTo)); }
        }

        private decimal _trialBalanceTotalDebit;
        public decimal TrialBalanceTotalDebit
        {
            get => _trialBalanceTotalDebit;
            private set
            {
                _trialBalanceTotalDebit = value;
                OnPropertyChanged(nameof(TrialBalanceTotalDebit));
                OnPropertyChanged(nameof(TrialBalanceSummary));
            }
        }

        private decimal _trialBalanceTotalCredit;
        public decimal TrialBalanceTotalCredit
        {
            get => _trialBalanceTotalCredit;
            private set
            {
                _trialBalanceTotalCredit = value;
                OnPropertyChanged(nameof(TrialBalanceTotalCredit));
                OnPropertyChanged(nameof(TrialBalanceSummary));
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value) return;
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private string _status = "Accounting center is ready.";
        public string Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        private Brush _statusBrush = Brushes.LightGreen;
        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        private bool _isStatusSuccess = true;
        public bool IsStatusSuccess
        {
            get => _isStatusSuccess;
            private set
            {
                if (_isStatusSuccess == value) return;
                _isStatusSuccess = value;
                OnPropertyChanged(nameof(IsStatusSuccess));
            }
        }

        public string AccountsSummary => $"{Accounts.Count} account(s) in the chart of accounts.";
        public string AccountTypesSummary => $"{AccountTypes.Count} account type definition(s) available.";
        public string PostingSettingsSummary => $"{PostingSettings.Count} posting role(s) are ready for sales and purchase automation.";
        public string PostingRolesSummary => $"{PostingRoleDefinitions.Count} posting role definition(s) available.";
        public string JournalSummary => $"{JournalEntries.Count} journal entr{(JournalEntries.Count == 1 ? "y" : "ies")} loaded.";
        public string SelectedJournalDetailSummary => SelectedJournalDetail == null
            ? "Select a journal entry to inspect its lines."
            : $"Debit {SelectedJournalDetail.TotalDebit:N2} | Credit {SelectedJournalDetail.TotalCredit:N2}";
        public string LedgerSummary => LedgerReport == null
            ? "Choose an account to view its running balance."
            : $"Opening {LedgerReport.OpeningBalance:N2} | Closing {LedgerReport.ClosingBalance:N2}";
        public string TrialBalanceSummary => $"Debit {TrialBalanceTotalDebit:N2} | Credit {TrialBalanceTotalCredit:N2}";

        public ICommand LoadCommand { get; }
        public ICommand LoadSetupCommand { get; }
        public ICommand LoadReviewCommand { get; }
        public ICommand LoadManualJournalCommand { get; }
        public ICommand StartNewAccountCommand { get; }
        public ICommand SaveAccountCommand { get; }
        public ICommand DeleteAccountCommand { get; }
        public ICommand StartNewAccountTypeCommand { get; }
        public ICommand SaveAccountTypeCommand { get; }
        public ICommand DeleteAccountTypeCommand { get; }
        public ICommand StartNewPostingRoleCommand { get; }
        public ICommand SavePostingRoleCommand { get; }
        public ICommand DeletePostingRoleCommand { get; }
        public ICommand SavePostingSettingsCommand { get; }
        public ICommand RefreshJournalCommand { get; }
        public ICommand AddManualLineCommand { get; }
        public ICommand RemoveManualLineCommand { get; }
        public ICommand PostManualJournalCommand { get; }
        public ICommand RefreshLedgerCommand { get; }
        public ICommand RefreshTrialBalanceCommand { get; }

        public AccountingViewModel(IAccountingApiClient accountingApi)
        {
            _accountingApi = accountingApi;

            LoadSetupCommand = new RelayCommand(_ => _ = LoadSetupAsync());
            LoadReviewCommand = new RelayCommand(_ => _ = LoadReviewAsync());
            LoadManualJournalCommand = new RelayCommand(_ => _ = LoadManualJournalAsync());
            LoadCommand = LoadReviewCommand;
            StartNewAccountCommand = new RelayCommand(_ => StartNewAccount());
            SaveAccountCommand = new RelayCommand(_ => _ = SaveAccountAsync());
            DeleteAccountCommand = new RelayCommand(_ => _ = DeleteAccountAsync());
            StartNewAccountTypeCommand = new RelayCommand(_ => StartNewAccountTypeDefinition());
            SaveAccountTypeCommand = new RelayCommand(_ => _ = SaveAccountTypeAsync());
            DeleteAccountTypeCommand = new RelayCommand(_ => _ = DeleteAccountTypeAsync());
            StartNewPostingRoleCommand = new RelayCommand(_ => StartNewPostingRoleDefinition());
            SavePostingRoleCommand = new RelayCommand(_ => _ = SavePostingRoleAsync());
            DeletePostingRoleCommand = new RelayCommand(_ => _ = DeletePostingRoleAsync());
            SavePostingSettingsCommand = new RelayCommand(_ => _ = SavePostingSettingsAsync());
            RefreshJournalCommand = new RelayCommand(_ => _ = RefreshJournalsAsync());
            AddManualLineCommand = new RelayCommand(_ => AddManualLine());
            RemoveManualLineCommand = new RelayCommand(_ => RemoveSelectedManualLine());
            PostManualJournalCommand = new RelayCommand(_ => _ = PostManualJournalAsync());
            RefreshLedgerCommand = new RelayCommand(_ => _ = RefreshLedgerAsync());
            RefreshTrialBalanceCommand = new RelayCommand(_ => _ = RefreshTrialBalanceAsync());

            EnsureManualJournalSeedLines();
        }

        public async Task LoadAsync()
            => await LoadReviewAsync();

        public async Task LoadSetupAsync()
            => await LoadAsyncCore(AccountingLoadScope.Setup);

        public async Task LoadReviewAsync()
            => await LoadAsyncCore(AccountingLoadScope.Review);

        public async Task LoadManualJournalAsync()
            => await LoadAsyncCore(AccountingLoadScope.ManualJournal);

        private async Task LoadAsyncCore(AccountingLoadScope scope, int? accountIdToSelect = null, int? journalIdToSelect = null)
        {
            IsLoading = true;
            SetStatus(scope switch
            {
                AccountingLoadScope.Setup => "Loading accounting setup…",
                AccountingLoadScope.Review => "Loading accounting review…",
                AccountingLoadScope.ManualJournal => "Loading manual journal workspace…",
                _ => "Loading accounting workspace…"
            }, true);

            try
            {
                var preservedAccountId = accountIdToSelect ?? SelectedAccount?.Id;
                var preservedLedgerAccountId = SelectedLedgerAccountId;
                var preservedJournalId = journalIdToSelect ?? SelectedJournalEntry?.Id;
                var preservedAccountTypeDefinitionKey = SelectedAccountTypeDefinition?.TypeKey;
                var preservedPostingRoleKey = SelectedPostingRoleDefinition?.RoleKey;

                Task<List<AccountDto>>? accountsTask = null;
                Task<List<AccountTypeDefinitionDto>>? accountTypesTask = null;
                Task<List<PostingAccountSettingDto>>? settingsTask = null;
                Task<List<PostingRoleDefinitionDto>>? postingRolesTask = null;
                Task<List<JournalEntrySummaryDto>>? journalsTask = null;
                Task<TrialBalanceReportDto>? trialBalanceTask = null;

                if (scope is AccountingLoadScope.Setup or AccountingLoadScope.Review or AccountingLoadScope.ManualJournal)
                {
                    accountsTask = _accountingApi.GetAccountsAsync();
                }

                if (scope == AccountingLoadScope.Setup)
                {
                    accountTypesTask = _accountingApi.GetAccountTypesAsync();
                    postingRolesTask = _accountingApi.GetPostingRolesAsync();
                    settingsTask = _accountingApi.GetPostingSettingsAsync();
                }

                if (scope == AccountingLoadScope.Review)
                {
                    journalsTask = _accountingApi.GetJournalEntriesAsync(JournalDateFrom, JournalDateTo);
                    trialBalanceTask = _accountingApi.GetTrialBalanceAsync(TrialBalanceDateFrom, TrialBalanceDateTo);
                }

                var tasks = new List<Task>();
                if (accountsTask != null) tasks.Add(accountsTask);
                if (accountTypesTask != null) tasks.Add(accountTypesTask);
                if (settingsTask != null) tasks.Add(settingsTask);
                if (postingRolesTask != null) tasks.Add(postingRolesTask);
                if (journalsTask != null) tasks.Add(journalsTask);
                if (trialBalanceTask != null) tasks.Add(trialBalanceTask);

                await Task.WhenAll(tasks);

                if (accountTypesTask != null)
                {
                    ApplyAccountTypes(accountTypesTask.Result, preservedAccountTypeDefinitionKey);
                }

                if (accountsTask != null)
                {
                    ApplyAccounts(accountsTask.Result, preservedAccountId);
                }

                if (postingRolesTask != null)
                {
                    ApplyPostingRoleDefinitions(postingRolesTask.Result, preservedPostingRoleKey);
                }

                if (settingsTask != null)
                {
                    ApplyPostingSettings(settingsTask.Result);
                }

                if (journalsTask != null && trialBalanceTask != null)
                {
                    ApplyReviewData(
                        journalsTask.Result,
                        trialBalanceTask.Result,
                        preservedLedgerAccountId,
                        preservedJournalId);

                    await LoadSelectedJournalDetailAsync();
                    await RefreshLedgerAsync(silent: true);
                }

                SetStatus(scope switch
                {
                    AccountingLoadScope.Setup => "Accounting setup loaded.",
                    AccountingLoadScope.Review => "Accounting review loaded.",
                    AccountingLoadScope.ManualJournal => "Manual journal workspace loaded.",
                    _ => "Accounting workspace loaded."
                }, true);
            }
            catch (Exception ex)
            {
                SetStatus(scope switch
                {
                    AccountingLoadScope.Setup => $"Loading accounting setup failed: {ex.Message}",
                    AccountingLoadScope.Review => $"Loading accounting review failed: {ex.Message}",
                    AccountingLoadScope.ManualJournal => $"Loading manual journal workspace failed: {ex.Message}",
                    _ => $"Accounting load failed: {ex.Message}"
                }, false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyAccounts(IEnumerable<AccountDto> accounts, int? preservedAccountId)
        {
            Replace(Accounts, accounts.OrderBy(account => account.Code).ToList());
            Replace(ParentAccountOptions, new[]
            {
                new AccountSelectorOption { Id = null, DisplayName = "Root account" }
            }.Concat(Accounts.Select(account => new AccountSelectorOption
            {
                Id = account.Id,
                DisplayName = account.DisplayName
            })));
            Replace(PostingAccountOptions, new[]
            {
                new AccountSelectorOption { Id = null, DisplayName = "Not configured" }
            }.Concat(Accounts.Select(account => new AccountSelectorOption
            {
                Id = account.Id,
                DisplayName = account.DisplayName
            })));

            SelectedAccount = ResolveAccountSelection(preservedAccountId);
            EnsureManualJournalLineAccounts();

            OnPropertyChanged(nameof(AccountsSummary));
        }

        private void ApplyAccountTypes(IEnumerable<AccountTypeDefinitionDto> accountTypes, string? preservedTypeKey)
        {
            Replace(AccountTypes, accountTypes.OrderBy(type => type.SortOrder).ThenBy(type => type.Label).ToList());
            SelectedAccountTypeDefinition = ResolveAccountTypeDefinitionSelection(preservedTypeKey);

            if (!AccountTypes.Any(type => string.Equals(type.TypeKey, SelectedAccountTypeKey, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedAccountTypeKey = AccountTypes.FirstOrDefault()?.TypeKey ?? "asset";
            }

            OnPropertyChanged(nameof(AccountTypesSummary));
        }

        private void ApplyPostingRoleDefinitions(IEnumerable<PostingRoleDefinitionDto> postingRoles, string? preservedRoleKey)
        {
            Replace(PostingRoleDefinitions, postingRoles.OrderBy(role => role.SortOrder).ThenBy(role => role.Label).ToList());
            SelectedPostingRoleDefinition = ResolvePostingRoleDefinitionSelection(preservedRoleKey);
            OnPropertyChanged(nameof(PostingRolesSummary));
        }

        private void ApplyPostingSettings(IEnumerable<PostingAccountSettingDto> postingSettings)
        {
            Replace(PostingSettings, postingSettings.Select(item => new PostingSettingEditorRow
            {
                SettingKey = item.SettingKey,
                Label = item.Label,
                Description = item.Description,
                SelectedAccountId = item.AccountId
            }).ToList());

            OnPropertyChanged(nameof(PostingSettingsSummary));
        }

        private void ApplyReviewData(
            IEnumerable<JournalEntrySummaryDto> journals,
            TrialBalanceReportDto trialBalance,
            int? preservedLedgerAccountId,
            int? preservedJournalId)
        {
            Replace(JournalEntries, journals);
            Replace(TrialBalanceRows, trialBalance.Rows);

            TrialBalanceTotalDebit = trialBalance.TotalDebit;
            TrialBalanceTotalCredit = trialBalance.TotalCredit;

            _suppressJournalSelectionLoad = true;
            _suppressLedgerRefresh = true;

            SelectedLedgerAccountId = ResolveAccountSelection(preservedLedgerAccountId)?.Id ?? Accounts.FirstOrDefault()?.Id;
            SelectedJournalEntry = ResolveJournalSelection(preservedJournalId);

            _suppressJournalSelectionLoad = false;
            _suppressLedgerRefresh = false;

            OnPropertyChanged(nameof(JournalSummary));
            OnPropertyChanged(nameof(TrialBalanceSummary));
        }

        private async Task SaveAccountAsync()
        {
            try
            {
                var request = new CreateAccountRequest
                {
                    Code = (AccountCode ?? string.Empty).Trim(),
                    Name = (AccountName ?? string.Empty).Trim(),
                    AccountTypeKey = SelectedAccountTypeKey,
                    ParentId = SelectedParentAccountId
                };

                var isUpdate = SelectedAccount is { Id: > 0 };
                int accountId;
                if (SelectedAccount is { Id: > 0 } selected)
                {
                    await _accountingApi.UpdateAccountAsync(selected.Id, request);
                    accountId = selected.Id;
                }
                else
                {
                    accountId = await _accountingApi.CreateAccountAsync(request);
                }

                await LoadAsyncCore(AccountingLoadScope.Setup, accountId);
                SetStatus(isUpdate
                    ? $"Account {request.Code} updated."
                    : $"Account {request.Code} created.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Saving account failed: {ex.Message}", false);
            }
        }

        private async Task DeleteAccountAsync()
        {
            if (SelectedAccount is not { Id: > 0 } selected)
            {
                SetStatus("Select an account to delete.", false);
                return;
            }

            try
            {
                await _accountingApi.DeleteAccountAsync(selected.Id);
                StartNewAccount();
                await LoadAsyncCore(AccountingLoadScope.Setup);
                SetStatus($"Account {selected.Code} deleted.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Deleting account failed: {ex.Message}", false);
            }
        }

        private async Task SaveAccountTypeAsync()
        {
            try
            {
                var request = new SaveAccountTypeRequest
                {
                    TypeKey = (AccountTypeDefinitionKey ?? string.Empty).Trim(),
                    Label = (AccountTypeDefinitionLabel ?? string.Empty).Trim(),
                    Description = (AccountTypeDefinitionDescription ?? string.Empty).Trim(),
                    SortOrder = AccountTypeDefinitionSortOrder
                };

                var isUpdate = SelectedAccountTypeDefinition != null;
                var typeKey = isUpdate
                    ? SelectedAccountTypeDefinition!.TypeKey
                    : await _accountingApi.CreateAccountTypeAsync(request);

                if (isUpdate)
                {
                    await _accountingApi.UpdateAccountTypeAsync(typeKey, request);
                }

                await LoadAsyncCore(AccountingLoadScope.Setup, SelectedAccount?.Id);
                SelectedAccountTypeDefinition = ResolveAccountTypeDefinitionSelection(typeKey);
                SetStatus(isUpdate
                    ? $"Account type {typeKey} updated."
                    : $"Account type {typeKey} created.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Saving account type failed: {ex.Message}", false);
            }
        }

        private async Task DeleteAccountTypeAsync()
        {
            if (SelectedAccountTypeDefinition == null)
            {
                SetStatus("Select an account type to delete.", false);
                return;
            }

            try
            {
                var typeKey = SelectedAccountTypeDefinition.TypeKey;
                await _accountingApi.DeleteAccountTypeAsync(typeKey);
                StartNewAccountTypeDefinition();
                await LoadAsyncCore(AccountingLoadScope.Setup, SelectedAccount?.Id);
                SetStatus($"Account type {typeKey} deleted.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Deleting account type failed: {ex.Message}", false);
            }
        }

        private async Task SavePostingRoleAsync()
        {
            try
            {
                var request = new SavePostingRoleRequest
                {
                    RoleKey = (PostingRoleDefinitionKey ?? string.Empty).Trim(),
                    Label = (PostingRoleDefinitionLabel ?? string.Empty).Trim(),
                    Description = (PostingRoleDefinitionDescription ?? string.Empty).Trim(),
                    SortOrder = PostingRoleDefinitionSortOrder
                };

                var isUpdate = SelectedPostingRoleDefinition != null;
                var roleKey = isUpdate
                    ? SelectedPostingRoleDefinition!.RoleKey
                    : await _accountingApi.CreatePostingRoleAsync(request);

                if (isUpdate)
                {
                    await _accountingApi.UpdatePostingRoleAsync(roleKey, request);
                }

                await LoadAsyncCore(AccountingLoadScope.Setup, SelectedAccount?.Id);
                SelectedPostingRoleDefinition = ResolvePostingRoleDefinitionSelection(roleKey);
                SetStatus(isUpdate
                    ? $"Posting role {roleKey} updated."
                    : $"Posting role {roleKey} created.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Saving posting role failed: {ex.Message}", false);
            }
        }

        private async Task DeletePostingRoleAsync()
        {
            if (SelectedPostingRoleDefinition == null)
            {
                SetStatus("Select a posting role to delete.", false);
                return;
            }

            try
            {
                var roleKey = SelectedPostingRoleDefinition.RoleKey;
                await _accountingApi.DeletePostingRoleAsync(roleKey);
                StartNewPostingRoleDefinition();
                await LoadAsyncCore(AccountingLoadScope.Setup, SelectedAccount?.Id);
                SetStatus($"Posting role {roleKey} deleted.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Deleting posting role failed: {ex.Message}", false);
            }
        }

        private async Task SavePostingSettingsAsync()
        {
            try
            {
                var request = new UpdatePostingSettingsRequest
                {
                    Items = PostingSettings.Select(row => new UpdatePostingSettingItemRequest
                    {
                        SettingKey = row.SettingKey,
                        AccountId = row.SelectedAccountId
                    }).ToList()
                };

                await _accountingApi.SavePostingSettingsAsync(request);
                await LoadAsyncCore(AccountingLoadScope.Setup, SelectedAccount?.Id);
                SetStatus("Posting setup saved.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Saving posting setup failed: {ex.Message}", false);
            }
        }

        private async Task RefreshJournalsAsync()
        {
            try
            {
                Replace(JournalEntries, await _accountingApi.GetJournalEntriesAsync(JournalDateFrom, JournalDateTo));
                SelectedJournalEntry = ResolveJournalSelection(SelectedJournalEntry?.Id);
                await LoadSelectedJournalDetailAsync();
                OnPropertyChanged(nameof(JournalSummary));
                SetStatus("Journal list refreshed.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Refreshing journals failed: {ex.Message}", false);
            }
        }

        private async Task LoadSelectedJournalDetailAsync()
        {
            try
            {
                if (SelectedJournalEntry == null)
                {
                    SelectedJournalDetail = null;
                    Replace(SelectedJournalLines, Array.Empty<JournalEntryLineDto>());
                    return;
                }

                var detail = await _accountingApi.GetJournalEntryAsync(SelectedJournalEntry.Id);
                SelectedJournalDetail = detail;
                Replace(SelectedJournalLines, detail.Lines);
            }
            catch (Exception ex)
            {
                SelectedJournalDetail = null;
                Replace(SelectedJournalLines, Array.Empty<JournalEntryLineDto>());
                SetStatus($"Loading journal details failed: {ex.Message}", false);
            }
        }

        private async Task PostManualJournalAsync()
        {
            try
            {
                var request = new CreateManualJournalEntryRequest
                {
                    EntryDate = ManualJournalEntryDate,
                    Description = (ManualJournalDescription ?? string.Empty).Trim(),
                    ReferenceType = string.IsNullOrWhiteSpace(ManualJournalReferenceType) ? "Manual" : ManualJournalReferenceType.Trim(),
                    Lines = ManualJournalLines.Select(line => new CreateManualJournalLineRequest
                    {
                        AccountId = line.AccountId,
                        Debit = line.Debit,
                        Credit = line.Credit
                    }).ToList()
                };

                await _accountingApi.CreateManualJournalAsync(request);
                ResetManualJournalForm();
                await LoadAsyncCore(AccountingLoadScope.ManualJournal);
                SetStatus("Manual journal posted.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Posting manual journal failed: {ex.Message}", false);
            }
        }

        private async Task RefreshLedgerAsync(bool silent = false)
        {
            try
            {
                if (SelectedLedgerAccountId is not int accountId || accountId <= 0)
                {
                    LedgerReport = null;
                    Replace(LedgerEntries, Array.Empty<LedgerRowDto>());
                    return;
                }

                var report = await _accountingApi.GetLedgerAsync(accountId, LedgerDateFrom, LedgerDateTo);
                LedgerReport = report;
                Replace(LedgerEntries, report.Entries);
                OnPropertyChanged(nameof(LedgerSummary));

                if (!silent)
                {
                    SetStatus("Ledger refreshed.", true);
                }
            }
            catch (Exception ex)
            {
                LedgerReport = null;
                Replace(LedgerEntries, Array.Empty<LedgerRowDto>());
                SetStatus($"Refreshing ledger failed: {ex.Message}", false);
            }
        }

        private async Task RefreshTrialBalanceAsync()
        {
            try
            {
                var report = await _accountingApi.GetTrialBalanceAsync(TrialBalanceDateFrom, TrialBalanceDateTo);
                Replace(TrialBalanceRows, report.Rows);
                TrialBalanceTotalDebit = report.TotalDebit;
                TrialBalanceTotalCredit = report.TotalCredit;
                OnPropertyChanged(nameof(TrialBalanceSummary));
                SetStatus("Trial balance refreshed.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Refreshing trial balance failed: {ex.Message}", false);
            }
        }

        private void StartNewAccount()
        {
            _selectedAccount = null;
            OnPropertyChanged(nameof(SelectedAccount));
            AccountCode = string.Empty;
            AccountName = string.Empty;
            SelectedAccountTypeKey = AccountTypes.FirstOrDefault()?.TypeKey ?? "asset";
            SelectedParentAccountId = null;
        }

        private void StartNewAccountTypeDefinition()
        {
            _selectedAccountTypeDefinition = null;
            OnPropertyChanged(nameof(SelectedAccountTypeDefinition));
            OnPropertyChanged(nameof(IsEditingAccountTypeDefinition));
            AccountTypeDefinitionKey = string.Empty;
            AccountTypeDefinitionLabel = string.Empty;
            AccountTypeDefinitionDescription = string.Empty;
            AccountTypeDefinitionSortOrder = AccountTypes.Count == 0
                ? 10
                : AccountTypes.Max(type => type.SortOrder) + 10;
        }

        private void PopulateAccountTypeDefinitionForm(AccountTypeDefinitionDto? definition)
        {
            if (definition == null)
            {
                AccountTypeDefinitionKey = string.Empty;
                AccountTypeDefinitionLabel = string.Empty;
                AccountTypeDefinitionDescription = string.Empty;
                AccountTypeDefinitionSortOrder = AccountTypes.Count == 0
                    ? 10
                    : AccountTypes.Max(type => type.SortOrder) + 10;
                return;
            }

            AccountTypeDefinitionKey = definition.TypeKey;
            AccountTypeDefinitionLabel = definition.Label;
            AccountTypeDefinitionDescription = definition.Description;
            AccountTypeDefinitionSortOrder = definition.SortOrder;
        }

        private void StartNewPostingRoleDefinition()
        {
            _selectedPostingRoleDefinition = null;
            OnPropertyChanged(nameof(SelectedPostingRoleDefinition));
            OnPropertyChanged(nameof(IsEditingPostingRoleDefinition));
            PostingRoleDefinitionKey = string.Empty;
            PostingRoleDefinitionLabel = string.Empty;
            PostingRoleDefinitionDescription = string.Empty;
            PostingRoleDefinitionSortOrder = PostingRoleDefinitions.Count == 0
                ? 10
                : PostingRoleDefinitions.Max(role => role.SortOrder) + 10;
        }

        private void PopulatePostingRoleDefinitionForm(PostingRoleDefinitionDto? definition)
        {
            if (definition == null)
            {
                PostingRoleDefinitionKey = string.Empty;
                PostingRoleDefinitionLabel = string.Empty;
                PostingRoleDefinitionDescription = string.Empty;
                PostingRoleDefinitionSortOrder = PostingRoleDefinitions.Count == 0
                    ? 10
                    : PostingRoleDefinitions.Max(role => role.SortOrder) + 10;
                return;
            }

            PostingRoleDefinitionKey = definition.RoleKey;
            PostingRoleDefinitionLabel = definition.Label;
            PostingRoleDefinitionDescription = definition.Description;
            PostingRoleDefinitionSortOrder = definition.SortOrder;
        }

        private void PopulateAccountForm(AccountDto? account)
        {
            if (account == null)
            {
                AccountCode = string.Empty;
                AccountName = string.Empty;
                SelectedAccountTypeKey = AccountTypes.FirstOrDefault()?.TypeKey ?? "asset";
                SelectedParentAccountId = null;
                return;
            }

            AccountCode = account.Code;
            AccountName = account.Name;
            SelectedAccountTypeKey = account.AccountTypeKey;
            SelectedParentAccountId = account.ParentId;
        }

        private AccountDto? ResolveAccountSelection(int? accountId)
            => accountId is int selectedId
                ? Accounts.FirstOrDefault(account => account.Id == selectedId) ?? Accounts.FirstOrDefault()
                : Accounts.FirstOrDefault();

        private AccountTypeDefinitionDto? ResolveAccountTypeDefinitionSelection(string? typeKey)
            => !string.IsNullOrWhiteSpace(typeKey)
                ? AccountTypes.FirstOrDefault(type => string.Equals(type.TypeKey, typeKey, StringComparison.OrdinalIgnoreCase))
                    ?? AccountTypes.FirstOrDefault()
                : AccountTypes.FirstOrDefault();

        private PostingRoleDefinitionDto? ResolvePostingRoleDefinitionSelection(string? roleKey)
            => !string.IsNullOrWhiteSpace(roleKey)
                ? PostingRoleDefinitions.FirstOrDefault(role => string.Equals(role.RoleKey, roleKey, StringComparison.OrdinalIgnoreCase))
                    ?? PostingRoleDefinitions.FirstOrDefault()
                : PostingRoleDefinitions.FirstOrDefault();

        private JournalEntrySummaryDto? ResolveJournalSelection(int? journalId)
            => journalId is int selectedId
                ? JournalEntries.FirstOrDefault(entry => entry.Id == selectedId) ?? JournalEntries.FirstOrDefault()
                : JournalEntries.FirstOrDefault();

        private void AddManualLine()
        {
            ManualJournalLines.Add(new ManualJournalLineEditor
            {
                AccountId = Accounts.FirstOrDefault()?.Id ?? 0
            });
        }

        private void RemoveSelectedManualLine()
        {
            if (SelectedManualJournalLine == null)
            {
                return;
            }

            ManualJournalLines.Remove(SelectedManualJournalLine);
            if (ManualJournalLines.Count == 0)
            {
                EnsureManualJournalSeedLines();
            }
        }

        private void EnsureManualJournalSeedLines()
        {
            if (ManualJournalLines.Count > 0)
            {
                return;
            }

            ManualJournalLines.Add(new ManualJournalLineEditor());
            ManualJournalLines.Add(new ManualJournalLineEditor());
        }

        private void EnsureManualJournalLineAccounts()
        {
            var fallbackAccountId = Accounts.FirstOrDefault()?.Id ?? 0;
            if (fallbackAccountId <= 0)
            {
                return;
            }

            var validAccountIds = Accounts.Select(account => account.Id).ToHashSet();
            foreach (var line in ManualJournalLines)
            {
                if (line.AccountId <= 0 || !validAccountIds.Contains(line.AccountId))
                {
                    line.AccountId = fallbackAccountId;
                }
            }
        }

        private void ResetManualJournalForm()
        {
            ManualJournalEntryDate = DateTime.Today;
            ManualJournalDescription = string.Empty;
            ManualJournalReferenceType = "Manual";
            ManualJournalLines.Clear();
            EnsureManualJournalSeedLines();
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
            IsStatusSuccess = isSuccess;
        }

        private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
        {
            collection.Clear();
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
