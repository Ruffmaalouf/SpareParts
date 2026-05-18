using System;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderAggregateRow : INotifyPropertyChanged
    {
        private ReportBuilderAggregateTypeOption? _selectedAggregateType;
        private ReportBuilderColumnOption? _selectedColumn;
        private string _alias = string.Empty;

        public ReportBuilderAggregateTypeOption? SelectedAggregateType
        {
            get => _selectedAggregateType;
            set
            {
                if (ReferenceEquals(_selectedAggregateType, value)) return;
                _selectedAggregateType = value;
                OnPropertyChanged(nameof(SelectedAggregateType));
                OnPropertyChanged(nameof(NeedsSourceColumn));
            }
        }

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

        public string Alias
        {
            get => _alias;
            set
            {
                if (_alias == value) return;
                _alias = value;
                OnPropertyChanged(nameof(Alias));
            }
        }

        public bool NeedsSourceColumn
            => !string.Equals(SelectedAggregateType?.Key, "COUNT_ROWS", StringComparison.OrdinalIgnoreCase);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
