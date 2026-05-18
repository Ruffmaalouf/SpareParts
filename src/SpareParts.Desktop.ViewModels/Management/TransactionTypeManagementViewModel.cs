using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class TransactionTypeManagementViewModel : ManagementFeatureViewModelBase
    {
        private ManagementCoordinator? _coordinator;
        private Func<Task>? _refreshAsync;
        private Action<string, bool>? _setStatus;
        private Func<string>? _getDefaultCounterCurrencyCode;
        private string _newTransactionTypeName = string.Empty;
        private string _newTransactionCurrencyCode = "USD";
        private decimal _newTransactionCounterRate = 1m;
        private string _newTransactionSerialNumberFormat = "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}";
        private long _newTransactionStartNumber = 1;
        private long _newTransactionCurrentNumber;
        private bool _newTransactionIsActive = true;
        private TransactionTypeDto? _selectedTransactionType;
        private bool _canViewTransactionTypesTab;

        public ObservableCollection<TransactionTypeDto> TransactionTypes { get; } = new();
        public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
        public ICommand ImportFromExcelCommand { get; private set; } = new RelayCommand(_ => { });

        public bool CanViewTransactionTypesTab
        {
            get => _canViewTransactionTypesTab;
            set => SetProperty(ref _canViewTransactionTypesTab, value);
        }

        public void Configure(
            ManagementCoordinator coordinator,
            Func<Task> refreshAsync,
            Action<string, bool> setStatus,
            Func<string> getDefaultCounterCurrencyCode,
            ICommand? importTableCommand = null)
        {
            _coordinator = coordinator;
            _refreshAsync = refreshAsync;
            _setStatus = setStatus;
            _getDefaultCounterCurrencyCode = getDefaultCounterCurrencyCode;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.TransactionTypes"));
        }

        public string NewTransactionTypeName
        {
            get => _newTransactionTypeName;
            set => SetProperty(ref _newTransactionTypeName, value);
        }

        public string NewTransactionCurrencyCode
        {
            get => _newTransactionCurrencyCode;
            set => SetProperty(ref _newTransactionCurrencyCode, value);
        }

        public decimal NewTransactionCounterRate
        {
            get => _newTransactionCounterRate;
            set => SetProperty(ref _newTransactionCounterRate, value);
        }

        public string NewTransactionSerialNumberFormat
        {
            get => _newTransactionSerialNumberFormat;
            set => SetProperty(ref _newTransactionSerialNumberFormat, value);
        }

        public long NewTransactionStartNumber
        {
            get => _newTransactionStartNumber;
            set => SetProperty(ref _newTransactionStartNumber, value);
        }

        public long NewTransactionCurrentNumber
        {
            get => _newTransactionCurrentNumber;
            set => SetProperty(ref _newTransactionCurrentNumber, value);
        }

        public bool NewTransactionIsActive
        {
            get => _newTransactionIsActive;
            set => SetProperty(ref _newTransactionIsActive, value);
        }

        public TransactionTypeDto? SelectedTransactionType
        {
            get => _selectedTransactionType;
            set
            {
                if (!SetProperty(ref _selectedTransactionType, value))
                {
                    return;
                }

                if (value != null)
                {
                    PopulateForm(value);
                }
            }
        }

        public void PopulateForm(TransactionTypeDto dto)
        {
            NewTransactionTypeName = dto.Name;
            NewTransactionCurrencyCode = dto.CurrencyCode;
            NewTransactionCounterRate = dto.CounterRate;
            NewTransactionSerialNumberFormat = dto.SerialNumberFormat;
            NewTransactionStartNumber = dto.StartNumber;
            NewTransactionCurrentNumber = dto.CurrentNumber;
            NewTransactionIsActive = dto.IsActive;
        }

        public void ClearForm(string defaultCounterCurrencyCode = "USD")
        {
            NewTransactionTypeName = string.Empty;
            NewTransactionCurrencyCode = defaultCounterCurrencyCode;
            NewTransactionCounterRate = 1m;
            NewTransactionSerialNumberFormat = "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}";
            NewTransactionStartNumber = 1;
            NewTransactionCurrentNumber = 0;
            NewTransactionIsActive = true;
            SelectedTransactionType = null;
        }

        public void StartNew() => ClearForm(_getDefaultCounterCurrencyCode?.Invoke() ?? "USD");

        private async Task SaveAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.SaveTransactionTypeAsync(this);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            if (_coordinator == null || _refreshAsync == null || _setStatus == null)
            {
                return;
            }

            var result = await _coordinator.DeleteTransactionTypeAsync(SelectedTransactionType);
            _setStatus(result.Message, result.Success);
            if (!result.Success)
            {
                return;
            }

            await _refreshAsync();
            StartNew();
        }
    }
}
