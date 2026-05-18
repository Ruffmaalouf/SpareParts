using System;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderFilterRow : INotifyPropertyChanged
    {
        private ReportBuilderColumnOption? _selectedColumn;
        private ReportBuilderOperatorOption? _selectedOperator;
        private string _value = string.Empty;
        private string _valueTo = string.Empty;

        public ReportBuilderColumnOption? SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                if (ReferenceEquals(_selectedColumn, value)) return;
                _selectedColumn = value;
                OnPropertyChanged(nameof(SelectedColumn));
            }
        }

        public ReportBuilderOperatorOption? SelectedOperator
        {
            get => _selectedOperator;
            set
            {
                if (ReferenceEquals(_selectedOperator, value)) return;
                _selectedOperator = value;
                OnPropertyChanged(nameof(SelectedOperator));
                OnPropertyChanged(nameof(NeedsSecondValue));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }

        public string ValueTo
        {
            get => _valueTo;
            set
            {
                if (_valueTo == value) return;
                _valueTo = value;
                OnPropertyChanged(nameof(ValueTo));
            }
        }

        public bool NeedsSecondValue
            => string.Equals(SelectedOperator?.Key, "between", StringComparison.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
