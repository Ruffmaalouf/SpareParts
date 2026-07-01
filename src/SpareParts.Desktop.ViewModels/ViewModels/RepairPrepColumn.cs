using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RepairPrepColumn : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public RepairPrepColumn(string key, string label, int sortOrder)
        {
            Key = key;
            Label = label;
            SortOrder = sortOrder;
        }

        public string Key { get; }
        public string Label { get; }
        public int SortOrder { get; }
        public ObservableCollection<RepairPrepCarRow> Cars { get; } = new();
        public int Count => Cars.Count;

        public void OnCarsChanged()
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));

        public static IReadOnlyList<RepairPrepColumn> CreateDefaultColumns()
            => new[]
            {
                new RepairPrepColumn("bought", "Bought", 0),
                new RepairPrepColumn("inspected", "Inspected", 1),
                new RepairPrepColumn("parts-needed", "Parts Needed", 2),
                new RepairPrepColumn("repairing", "Repairing", 3),
                new RepairPrepColumn("photo-ready", "Photo-ready", 4),
                new RepairPrepColumn("listed", "Listed", 5),
                new RepairPrepColumn("sold", "Sold", 6)
            };
    }
}
