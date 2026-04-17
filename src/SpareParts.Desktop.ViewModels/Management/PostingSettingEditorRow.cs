using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class PostingSettingEditorRow : INotifyPropertyChanged
    {
        private int? _selectedAccountId;

        public string SettingKey { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;

        public int? SelectedAccountId
        {
            get => _selectedAccountId;
            set
            {
                if (_selectedAccountId == value) return;
                _selectedAccountId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedAccountId)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
