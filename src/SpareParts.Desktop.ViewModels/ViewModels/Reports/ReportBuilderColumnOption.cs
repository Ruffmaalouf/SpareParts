using SpareParts.Domain.Reports;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderColumnOption : INotifyPropertyChanged
    {
        private bool _isSelected;

        public ReportBuilderColumnOption(ReportColumnDto dto)
        {
            Dto = dto;
        }

        public ReportColumnDto Dto { get; }
        public string Key => Dto.Key;
        public string TableKey => Dto.TableKey;
        public string TableDisplayName => Dto.TableDisplayName;
        public string ColumnName => Dto.ColumnName;
        public string DisplayName => string.IsNullOrWhiteSpace(Dto.QualifiedDisplayName) ? Dto.DisplayName : Dto.QualifiedDisplayName;
        public string ShortDisplayName => Dto.DisplayName;
        public string QualifiedColumnName => Dto.QualifiedColumnName;
        public string SqlDataType => Dto.SqlDataType;
        public string DataType => Dto.DataType;
        public bool IsNullable => Dto.IsNullable;
        public string Description => $"{TableDisplayName} • {SqlDataType}";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
