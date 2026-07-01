using System.ComponentModel;
using System.Globalization;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RepairPrepTaskRow : INotifyPropertyChanged
    {
        private bool _isDone;

        public RepairPrepTaskRow(string title, decimal cost)
        {
            Title = title;
            Cost = cost;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get; }
        public decimal Cost { get; }
        public string CostLabel => Cost.ToString("N2", CultureInfo.CurrentCulture);

        public bool IsDone
        {
            get => _isDone;
            set
            {
                if (_isDone == value)
                {
                    return;
                }

                _isDone = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDone)));
            }
        }
    }
}
