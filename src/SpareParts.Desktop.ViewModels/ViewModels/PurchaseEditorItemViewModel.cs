using System;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PurchaseEditorItemViewModel : INotifyPropertyChanged
    {
        private int _partId;
        private string _description = string.Empty;
        private int _quantity;
        private decimal _unitCost;

        public int PartId
        {
            get => _partId;
            set
            {
                if (_partId == value) return;
                _partId = value;
                OnPropertyChanged(nameof(PartId));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value) return;
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                if (_unitCost == value) return;
                _unitCost = value;
                OnPropertyChanged(nameof(UnitCost));
                OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal LineTotal => decimal.Round(Quantity * UnitCost, 4, MidpointRounding.AwayFromZero);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
