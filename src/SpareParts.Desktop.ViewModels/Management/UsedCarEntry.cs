using System.ComponentModel;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public class UsedCarEntry : INotifyPropertyChanged
    {
        private string _car = string.Empty;
        private int _modelYear;
        private string _priceCurrency = "USD";
        private decimal _priceBase;
        private decimal _priceCounter;
        private string _location = string.Empty;
        private decimal _transportation;
        private string _partOut = string.Empty;
        private decimal _shipping;
        private decimal _customs;

        public string Car
        {
            get => _car;
            set => SetField(ref _car, value);
        }

        public int ModelYear
        {
            get => _modelYear;
            set => SetField(ref _modelYear, value);
        }

        public string PriceCurrency
        {
            get => _priceCurrency;
            set => SetField(ref _priceCurrency, value);
        }

        public decimal PriceBase
        {
            get => _priceBase;
            set => SetField(ref _priceBase, value);
        }

        public decimal PriceCounter
        {
            get => _priceCounter;
            set
            {
                if (SetField(ref _priceCounter, value))
                {
                    OnPropertyChanged(nameof(TotalBeforeShipping));
                }
            }
        }

        public string Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        public decimal Transportation
        {
            get => _transportation;
            set
            {
                if (SetField(ref _transportation, value))
                {
                    OnPropertyChanged(nameof(TotalBeforeShipping));
                }
            }
        }

        public decimal TotalBeforeShipping => PriceCounter + Transportation;

        public string PartOut
        {
            get => _partOut;
            set => SetField(ref _partOut, value);
        }

        public decimal Shipping
        {
            get => _shipping;
            set => SetField(ref _shipping, value);
        }

        public decimal Customs
        {
            get => _customs;
            set => SetField(ref _customs, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private bool SetField<T>(ref T field, T value, string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(string? propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
