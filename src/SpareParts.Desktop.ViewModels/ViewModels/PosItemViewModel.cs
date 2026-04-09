using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class PosItemViewModel : INotifyPropertyChanged
    {
        private int _partId;
        private string _description = string.Empty;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _baseAmount;

        public int PartId
        {
            get => _partId;
            set
            {
                if (_partId == value)
                {
                    return;
                }

                _partId = value;
                OnPropertyChanged(nameof(PartId));
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value)
                {
                    return;
                }

                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity == value)
                {
                    return;
                }

                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (_unitPrice == value)
                {
                    return;
                }

                _unitPrice = value;
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal BaseAmount
        {
            get => _baseAmount;
            set
            {
                if (_baseAmount == value)
                {
                    return;
                }

                _baseAmount = value;
                OnPropertyChanged(nameof(BaseAmount));
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
