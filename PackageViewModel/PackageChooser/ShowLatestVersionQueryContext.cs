using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> where T : IPackageSearchMetadata
    {
        private readonly SourceRepository _sourceRepository;
        private readonly string _searchText;
        private readonly SearchFilter _searchFilter;
        private readonly int _pageSize;
        private PackageSearchResource _packageSearchResouce;
        private RawSearchResourceV3 _rawPackageSearchResouce;
        private int _pageIndex;
        private int? _lastPageIndex;
        private int? _lastPageCount;

        public ShowLatestVersionQueryContext(SourceRepository sourceRepository, string search, bool showPreReleasePackages, int pageSize)
        {
            _sourceRepository = sourceRepository;
            _searchText = search;
            _searchFilter = new SearchFilter(showPreReleasePackages);
            _pageSize = pageSize;
        }

        #region IQueryContext<T> Members

        public int CurrentPage => _pageIndex;

        public int BeginPackage => _pageIndex * _pageSize + (_lastPageIndex == 0 && _lastPageCount == 0 ? 0 : 1);

        public int EndPackage => _pageIndex * _pageSize + (IsLastPage ? _lastPageCount.Value : _pageSize);

        public bool IsLastPage => _pageIndex == _lastPageIndex;

        public async Task<IList<T>> GetItemsForCurrentPage(CancellationToken token)
        {
            IEnumerable<IPackageSearchMetadata> result = null;

            if (_searchText?.StartsWith("id:", StringComparison.OrdinalIgnoreCase) == true)
            {
                var id = _searchText.Substring(3).Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    var findPackageByIdResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>();

                    var metadata = await findPackageByIdResource.GetMetadataAsync(id, _searchFilter.IncludePrerelease, true, NullSourceCacheContext.Instance, NullLogger.Instance, token);

                    result = metadata.OrderByDescending(m => m.Identity.Version).Take(1);
                }
                else
                {
                    result = Enumerable.Empty<IPackageSearchMetadata>();
                }
            }
            else
            {
                if (_packageSearchResouce == null && _rawPackageSearchResouce == null)
                {
                    _rawPackageSearchResouce = await _sourceRepository.GetResourceAsync<RawSearchResourceV3>(token);
                }
                if (_rawPackageSearchResouce != null)
                {
                    var json = await _rawPackageSearchResouce.Search(_searchText, _searchFilter, _pageIndex * _pageSize, _pageSize, NullLogger.Instance, token);

                    result = json.Select(s => s.FromJToken<PackageSearchMetadata>());
                }

                if (result == null)
                {
                    if (_packageSearchResouce == null)
                    {
                        _packageSearchResouce = await _sourceRepository.GetResourceAsync<PackageSearchResource>(token);
                    }

                    result = await _packageSearchResouce.SearchAsync(_searchText, _searchFilter, _pageIndex * _pageSize, _pageSize, NullLogger.Instance, token);
                }
            }

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
