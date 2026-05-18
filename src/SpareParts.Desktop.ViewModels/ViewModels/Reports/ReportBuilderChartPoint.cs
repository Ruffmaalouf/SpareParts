using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderChartPoint : INotifyPropertyChanged
    {
        private string _label = string.Empty;
        private decimal _value;
        private decimal _secondaryValue;
        private double _percentage;

        public string Label
        {
            get => _label;
            set
            {
                if (_label == value) return;
                _label = value;
                OnPropertyChanged(nameof(Label));
            }
        }

        public decimal Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public decimal SecondaryValue
        {
            get => _secondaryValue;
            set
            {
                if (_secondaryValue == value) return;
                _secondaryValue = value;
                OnPropertyChanged(nameof(SecondaryValue));
            }
        }

        public double Percentage
        {
            get => _percentage;
            set
            {
                if (_percentage.Equals(value)) return;
                _percentage = value;
                OnPropertyChanged(nameof(Percentage));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
