using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> where T : IPackageInfoType
    {
        private readonly int _pageSize;
        private readonly IQueryable<T> _source;
        private readonly Lazy<int> _totalItemCount;
        private int _pageIndex;
        private bool _showUnlistedPackages;

        public ShowLatestVersionQueryContext(IQueryable<T> source, bool showUnlistedPackages, int pageSize)
        {
            _source = source;
            _pageSize = pageSize;
            _showUnlistedPackages = showUnlistedPackages;
            _totalItemCount = new Lazy<int>(_source.Count);
        }

        private int PageCount
        {
            get { return (TotalItemCount + (_pageSize - 1)) / _pageSize; }
        }

        #region IQueryContext<T> Members

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public int TotalItemCount
        {
            get
            {
                try
                {
                    return _totalItemCount.Value;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        public int BeginPackage
        {
            get { return Math.Min(TotalItemCount, _pageIndex * _pageSize + 1); }
        }

        public int EndPackage
        {
            get { return Math.Min(TotalItemCount, (_pageIndex + 1) * _pageSize); }
        }

        public IEnumerable<T> GetItemsForCurrentPage()
        {
            var results = _source.Skip(_pageIndex * _pageSize).Take(_pageSize);
            foreach (var package in results)
            {
                package.ShowAll = false;
                yield return package;
            }
        }

        public bool MoveFirst()
        {
            _pageIndex = 0;
            return true;
        }

        public bool MoveNext()
        {
            if (_pageIndex < PageCount - 1)
            {
                _pageIndex++;
                return true;
            }

            return false;
        }

        public bool MovePrevious()
        {
            if (_pageIndex > 0)
            {
                _pageIndex--;
                return true;
            }
            return false;
        }

        public bool MoveLast()
        {
            _pageIndex = PageCount - 1;
            return true;
        }

        #endregion
    }
}