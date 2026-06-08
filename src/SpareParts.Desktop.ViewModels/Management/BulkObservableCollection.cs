using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SpareParts.Desktop.Wpf.Management;

internal sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
    public void ReplaceWith(IEnumerable<T> items)
    {
        var snapshot = items.ToList();

        CheckReentrancy();
        Items.Clear();
        foreach (var item in snapshot)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

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
