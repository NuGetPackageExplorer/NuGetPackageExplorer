using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PackageExplorerViewModel
{
    public class SortedCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs CountPropertyChangeEventArgs =
            new PropertyChangedEventArgs("Count");

        private readonly SortedSet<T> _items;

        public SortedCollection()
        {
            _items = new SortedSet<T>();
        }

        public SortedCollection(IComparer<T> comparer)
        {
            _items = new SortedSet<T>(comparer);
        }

        public SortedCollection(IEnumerable<T> collection, IComparer<T> comparer)
        {
            _items = new SortedSet<T>(collection, comparer);
        }

        #region ICollection<T> Members

        public void Add(T item)
        {
            var added = _items.Add(item);
            if (added)
            {
                var index = IndexOf(item);
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                RaiseCollectionChangedEvent(args);
            }
        }

        public void Clear()
        {
            _items.Clear();

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            RaiseCollectionChangedEvent(args);
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            var index = IndexOf(item);
            var successful = _items.Remove(item);
            if (successful)
            {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                RaiseCollectionChangedEvent(args);
            }
            return successful;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private int IndexOf(T item)
        {
            var index = 0;
            foreach (var t in _items)
            {
                if (t != null && t.Equals(item))
                {
                    return index;
                }
                index++;
            }

            return -1;
        }

        private void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);

            // if the collection changes, raise the Count property changed event too.
            RaiseCountPropertyChanged();
        }

        private void RaiseCountPropertyChanged()
        {
            PropertyChanged?.Invoke(this, CountPropertyChangeEventArgs);
        }
    }
}
