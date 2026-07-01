using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SpareParts.Desktop.Wpf.Management;

internal static class ObservableCollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> target, IEnumerable<T> items)
    {
        if (target is BulkObservableCollection<T> bulkCollection)
        {
            bulkCollection.ReplaceWith(items);
            return;
        }

        var snapshot = items.ToList();
        target.Clear();
        foreach (var item in snapshot)
        {
            target.Add(item);
        }
    }
}
