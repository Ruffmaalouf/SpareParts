using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Helpers;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class TransactionTypeManagementViewModel : ManagementFeatureViewModelBase
    {
        private readonly IManagementFeatureContext _ctx;
        private string _newTransactionTypeName = string.Empty;
        private string _newTransactionCurrencyCode = "USD";
        private decimal _newTransactionCounterRate = 1m;
        private string _newTransactionSerialNumberFormat = "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}";
        private long _newTransactionStartNumber = 1;
        private long _newTransactionCurrentNumber;
        private bool _newTransactionIsActive = true;
        private TransactionTypeDto? _selectedTransactionType;
        private bool _canViewTransactionTypesTab;

        public TransactionTypeManagementViewModel(IManagementFeatureContext context)
        {
            _ctx = context;
            SaveCommand = new RelayCommand(_ => _ = SaveAsync());
            DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
            StartNewCommand = new RelayCommand(_ => StartNew());
            RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
            ImportFromExcelCommand = new RelayCommand(_ => _ctx.ImportTableCommand?.Execute("dbo.TransactionTypes"));
        }

        public ObservableCollection<TransactionTypeDto> TransactionTypes { get; } = new();
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand StartNewCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ImportFromExcelCommand { get; }

        public bool CanViewTransactionTypesTab
        {
            get => _canViewTransactionTypesTab;
            set => SetProperty(ref _canViewTransactionTypesTab, value);
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

        public void StartNew() => ClearForm(_ctx.GetDefaultCurrencyCode());

        private async Task SaveAsync()
        {
            var result = await _ctx.Coordinator.SaveTransactionTypeAsync(this);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }

        private async Task DeleteAsync()
        {
            var result = await _ctx.Coordinator.DeleteTransactionTypeAsync(SelectedTransactionType);
            _ctx.SetStatus(result.Message, result.Success);
            if (!result.Success) return;
            await _ctx.RefreshAsync();
            StartNew();
        }
    }
}
