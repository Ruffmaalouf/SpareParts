using System.Collections.Generic;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class UsedCarEntry : INotifyPropertyChanged
    {
        private int _id;
        private int? _supplierId;
        private int? _carModelId;
        private int? _locationId;
        private string _supplierName = string.Empty;
        private string _car = string.Empty;
        private int _modelYear;
        private string _priceCurrency = "USD";
        private decimal _price;
        private decimal _priceBase;
        private decimal _priceCounter;
        private string _location = string.Empty;
        private decimal _transportation;
        private bool _isReceived;
        private bool _isShipped;
        private decimal _partOut;
        private decimal _shipping;
        private decimal _customs;
        private decimal _totalBeforeShipping;
        private decimal _grandTotalBase;
        private decimal _grandTotalCounter;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public int? SupplierId
        {
            get => _supplierId;
            set => SetField(ref _supplierId, value);
        }

        public string SupplierName
        {
            get => _supplierName;
            set => SetField(ref _supplierName, value);
        }

        public int? CarModelId
        {
            get => _carModelId;
            set => SetField(ref _carModelId, value);
        }

        public string Car
        {
            get => _car;
            set => SetField(ref _car, value);
        }

        public int? LocationId
        {
            get => _locationId;
            set => SetField(ref _locationId, value);
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

        public decimal Price
        {
            get => _price;
            set => SetField(ref _price, value);
        }

        public decimal PriceBase
        {
            get => _priceBase;
            set => SetField(ref _priceBase, value);
        }

        public decimal PriceCounter
        {
            get => _priceCounter;
            set => SetField(ref _priceCounter, value);
        }

        public string Location
        {
            get => _location;
            set => SetField(ref _location, value);
        }

        public decimal Transportation
        {
            get => _transportation;
            set => SetField(ref _transportation, value);
        }

        public bool IsReceived
        {
            get => _isReceived;
            set => SetField(ref _isReceived, value);
        }

        public bool IsShipped
        {
            get => _isShipped;
            set => SetField(ref _isShipped, value);
        }

        public decimal TotalBeforeShipping
        {
            get => _totalBeforeShipping;
            set => SetField(ref _totalBeforeShipping, value);
        }

        public decimal PartOut
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

        public decimal GrandTotalBase
        {
            get => _grandTotalBase;
            set => SetField(ref _grandTotalBase, value);
        }

        public decimal GrandTotalCounter
        {
            get => _grandTotalCounter;
            set => SetField(ref _grandTotalCounter, value);
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
