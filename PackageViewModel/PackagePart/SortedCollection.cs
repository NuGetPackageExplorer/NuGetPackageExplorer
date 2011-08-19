using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace PackageExplorerViewModel {
    public class SortedCollection<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged {

        private readonly SortedSet<T> _items;
        private static readonly PropertyChangedEventArgs CountPropertyChangeEventArgs = new PropertyChangedEventArgs("Count");

        public SortedCollection() {
            _items = new SortedSet<T>();
        }

        public SortedCollection(IComparer<T> comparer) {
            _items = new SortedSet<T>(comparer);
        }

        public SortedCollection(IEnumerable<T> collection, IComparer<T> comparer) {
            _items = new SortedSet<T>(collection, comparer);
        }

        public void Add(T item) {
            bool added = _items.Add(item);
            if (added) {
                int index = IndexOf(item);
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
                RaiseCollectionChangedEvent(args);
            }
        }

        public void Clear() {
            _items.Clear();

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            RaiseCollectionChangedEvent(args);
        }

        public bool Contains(T item) {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex) {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count {
            get { return _items.Count; }  
        }

        public bool IsReadOnly {
            get { return false; }
        }

        public bool Remove(T item) {
            int index = IndexOf(item);
            bool successful = _items.Remove(item);
            if (successful) {
                var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                RaiseCollectionChangedEvent(args);

            }
            return successful;
        }

        public IEnumerator<T> GetEnumerator() {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _items.GetEnumerator();
        }

        private int IndexOf(T item) {
            int index = 0;
            foreach (T t in _items) {
                if (t.Equals(item)) {
                    return index;
                }
                index++;
            }

            return -1;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void RaiseCollectionChangedEvent(NotifyCollectionChangedEventArgs args) {
            if (CollectionChanged != null) {
                CollectionChanged(this, args);
            }

            // if the collection changes, raise the Count property changed event too.
            RaiseCountPropertyChanged();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaiseCountPropertyChanged() {
            if (PropertyChanged != null) {
                PropertyChanged(this, CountPropertyChangeEventArgs);
            }
        }
    }
}