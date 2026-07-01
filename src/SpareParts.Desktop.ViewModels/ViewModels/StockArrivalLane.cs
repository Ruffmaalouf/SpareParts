using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class StockArrivalLane : INotifyPropertyChanged
    {
        public StockArrivalLane(string key, string title, string subtitle, Brush accentBrush)
        {
            Key = key;
            Title = title;
            Subtitle = subtitle;
            AccentBrush = accentBrush;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Key { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public Brush AccentBrush { get; }
        public ObservableCollection<StockArrivalOpportunity> Opportunities { get; } = new();
        public int Count => Opportunities.Count;

        public void Replace(IEnumerable<StockArrivalOpportunity> rows)
        {
            Opportunities.Clear();
            foreach (var row in rows)
            {
                Opportunities.Add(row);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        }
    }
}
