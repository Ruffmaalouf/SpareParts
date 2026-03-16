using SpareParts.Desktop.Wpf.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class InvoiceTabViewModel : INotifyPropertyChanged
    {
        private static int _counter;

        public int TabNumber { get; } = ++_counter;
        public string Header => $"Invoice #{TabNumber}";

        public ObservableCollection<PosItemViewModel> Items { get; } = new();

        private int? _customerId;
        public int? CustomerId
        {
            get => _customerId;
            set { _customerId = value; OnPropertyChanged(nameof(CustomerId)); }
        }

        private int _warehouseId = 1;
        public int WarehouseId
        {
            get => _warehouseId;
            set { _warehouseId = value; OnPropertyChanged(nameof(WarehouseId)); }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                _paidAmount = value;
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingAmount));
            }
        }

        public decimal TotalAmount => Items.Sum(i => i.LineTotal);
        public decimal RemainingAmount => TotalAmount - PaidAmount;

        public ICommand SubmitSaleCommand { get; set; } = new RelayCommand(_ => { });

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
