using System;
using System.Collections.Generic;
using System.Linq;

namespace PackageExplorerViewModel {
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> {
        private readonly IQueryable<T> _source;
        private readonly int _pageSize;
        private readonly Lazy<int> _totalItemCount;
        private int _pageIndex;

        public ShowLatestVersionQueryContext(IQueryable<T> source, int pageSize) {
            _source = source;
            _pageSize = pageSize;
            _totalItemCount = new Lazy<int>(_source.Count);
        }

        public int TotalItemCount {
            get {
                return _totalItemCount.Value;
            }
        }

        private int PageCount {
            get {
                return (TotalItemCount + (_pageSize - 1)) / _pageSize;
            }
        }

        public int BeginPackage {
            get {
                return Math.Min(TotalItemCount, _pageIndex * _pageSize + 1);
            }
        }

        public int EndPackage {
            get {
                return Math.Min(TotalItemCount, (_pageIndex + 1) * _pageSize);
            }
        }

        public IEnumerable<T> GetItemsForCurrentPage() {
            return _source.Skip(Math.Max(0, BeginPackage - 1)).Take(_pageSize);
        }

        public bool MoveFirst() {
            _pageIndex = 0;
            return true;
        }

        public bool MoveNext() {
            if (_pageIndex < PageCount - 1) {
                _pageIndex++;
                return true;
            }

            return false;
        }

        public bool MovePrevious() {
            if (_pageIndex > 0) {
                _pageIndex--;
                return true;
            }
            return false;
        }

        public bool MoveLast() {
            _pageIndex = PageCount - 1;
            return true;
        }
    }
}