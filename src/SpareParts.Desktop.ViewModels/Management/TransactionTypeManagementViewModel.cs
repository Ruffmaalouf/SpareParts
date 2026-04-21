using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class TransactionTypeManagementViewModel
    {
        public ObservableCollection<TransactionTypeDto> TransactionTypes { get; } = new();

        public string NewTransactionTypeName { get; set; } = string.Empty;
        public string NewTransactionCurrencyCode { get; set; } = "USD";
        public decimal NewTransactionCounterRate { get; set; } = 1m;
        public string NewTransactionSerialNumberFormat { get; set; } = "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}";
        public long NewTransactionStartNumber { get; set; } = 1;
        public long NewTransactionCurrentNumber { get; set; }
        public bool NewTransactionIsActive { get; set; } = true;

        public TransactionTypeDto? SelectedTransactionType { get; set; }

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
    }
}
