using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderGroupByRow : INotifyPropertyChanged
    {
        private ReportBuilderColumnOption? _selectedColumn;

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

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
