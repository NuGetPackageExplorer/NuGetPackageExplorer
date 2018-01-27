using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> where T : IPackageSearchMetadata
    {
        private readonly PackageSearchResource _packageSearchResouce;
        private readonly string _searchText;
        private readonly SearchFilter _searchFilter;
        private readonly int _pageSize;
        private int _pageIndex;
        private int? _maxPageIndex;

        public ShowLatestVersionQueryContext(PackageSearchResource source, string search, SearchFilter filter, int pageSize)
        {
            _packageSearchResouce = source;
            _searchText = search;
            _searchFilter = filter;
            _pageSize = pageSize;
    }

        #region IQueryContext<T> Members

        public int CurrentPage { get { return _pageIndex; } }

        public async Task<IList<T>> GetItemsForCurrentPage(CancellationToken token)
        {
            var result = await _packageSearchResouce.SearchAsync(_searchText, _searchFilter, _pageIndex * _pageSize, _pageSize, NullLogger.Instance, token);

            token.ThrowIfCancellationRequested();

            var list = result.Cast<T>().ToList();

            if (list.Count < _pageSize)
            {
                _maxPageIndex = _pageIndex;
            }

            return list;
        }

        public bool MoveFirst()
        {
            _pageIndex = 0;
            return true;
        }

        public bool MoveNext()
        {
            if (!_maxPageIndex.HasValue || _pageIndex < _maxPageIndex)
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

        #endregion
    }
}
