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
        public bool NewTransactionIsActive { get; set; } = true;

        public TransactionTypeDto? SelectedTransactionType { get; set; }

        public void PopulateForm(TransactionTypeDto dto)
        {
            NewTransactionTypeName = dto.Name;
            NewTransactionCurrencyCode = dto.CurrencyCode;
            NewTransactionCounterRate = dto.CounterRate;
            NewTransactionIsActive = dto.IsActive;
        }

        public void ClearForm()
        {
            NewTransactionTypeName = string.Empty;
            NewTransactionCurrencyCode = "USD";
            NewTransactionCounterRate = 1m;
            NewTransactionIsActive = true;
            SelectedTransactionType = null;
        }
    }
}
