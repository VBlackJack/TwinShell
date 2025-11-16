using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace TwinShell.App.Collections;

/// <summary>
/// ObservableCollection with bulk operations support for better performance
/// BUGFIX: Added thread safety with lock to prevent race conditions during concurrent access
/// </summary>
public class ObservableRangeCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;
    private readonly object _lock = new object();

    /// <summary>
    /// Adds multiple items to the collection efficiently with a single notification
    /// </summary>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        lock (_lock)
        {
            _suppressNotification = true;

            foreach (var item in items)
            {
                Add(item);
            }

            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Replaces all items in the collection with new items efficiently
    /// </summary>
    public void ReplaceRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        lock (_lock)
        {
            _suppressNotification = true;

            Clear();
            foreach (var item in items)
            {
                Add(item);
            }

            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    /// <summary>
    /// Removes multiple items from the collection efficiently
    /// </summary>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        lock (_lock)
        {
            _suppressNotification = true;

            foreach (var item in items)
            {
                Remove(item);
            }

            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
        {
            base.OnCollectionChanged(e);
        }
    }
}
