using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportFieldMappingRow : INotifyPropertyChanged
    {
        private string? _selectedHeader;

        public required string FieldKey { get; init; }
        public required string ColumnName { get; init; }
        public required string Description { get; init; }
        public required string DataTypeLabel { get; init; }
        public required bool IsRequired { get; init; }

        public string RequirementLabel => IsRequired ? "Required" : "Optional";

        public string? SelectedHeader
        {
            get => _selectedHeader;
            set
            {
                if (_selectedHeader == value)
                {
                    return;
                }

                _selectedHeader = value;
                OnPropertyChanged(nameof(SelectedHeader));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
