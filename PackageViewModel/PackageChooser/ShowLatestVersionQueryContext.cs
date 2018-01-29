﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> where T : IPackageSearchMetadata
    {
        private readonly SourceRepository _sourceRepository;
        private readonly string _searchText;
        private readonly bool _showPreReleasePackages;
        private readonly int _pageSize;
        private PackageSearchResource _packageSearchResouce;
        private SearchFilter _searchFilter;
        private int _pageIndex;
        private int? _lastPageIndex;
        private int? _lastPageCount;

        public ShowLatestVersionQueryContext(SourceRepository sourceRepository, string search, bool showPreReleasePackages, int pageSize)
        {
            _sourceRepository = sourceRepository;
            _searchText = search;
            _showPreReleasePackages = showPreReleasePackages;
            _pageSize = pageSize;
    }

        #region IQueryContext<T> Members

        public int CurrentPage => _pageIndex;

        public int BeginPackage => _pageIndex * _pageSize;

        public int EndPackage => _pageIndex * _pageSize + (IsLast ? _lastPageCount.Value : _pageSize);

        public bool IsLast => _pageIndex == _lastPageIndex;

        public async Task<IList<T>> GetItemsForCurrentPage(CancellationToken token)
        {
            if (_packageSearchResouce == null)
            {
                _packageSearchResouce = await _sourceRepository.GetResourceAsync<PackageSearchResource>(token);
                _searchFilter = new SearchFilter(_showPreReleasePackages);
            }

            var result = await _packageSearchResouce.SearchAsync(_searchText, _searchFilter, _pageIndex * _pageSize, _pageSize, NullLogger.Instance, token);

            token.ThrowIfCancellationRequested();

            var list = result.Cast<T>().ToList();

            if (list.Count < _pageSize)
            {
                _lastPageIndex = _pageIndex;
                _lastPageCount = list.Count;
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
            if (!_lastPageIndex.HasValue || _pageIndex < _lastPageIndex)
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
