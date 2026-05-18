using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderKpiCard : INotifyPropertyChanged
    {
        private string _label = string.Empty;
        private string _value = string.Empty;
        private string _hint = string.Empty;

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

        public string Hint
        {
            get => _hint;
            set
            {
                if (_hint == value) return;
                _hint = value;
                OnPropertyChanged(nameof(Hint));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
