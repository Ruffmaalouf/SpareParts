using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderCalculatedFieldRow : INotifyPropertyChanged
    {
        private string _alias = string.Empty;
        private string _expression = string.Empty;

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

        public string Expression
        {
            get => _expression;
            set
            {
                if (_expression == value) return;
                _expression = value;
                OnPropertyChanged(nameof(Expression));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
