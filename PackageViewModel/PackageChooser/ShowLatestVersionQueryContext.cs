using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using PackageExplorerViewModel.PackageSearch;

namespace PackageExplorerViewModel
{
    internal class ShowLatestVersionQueryContext<T> : IQueryContext<T> where T : IPackageSearchMetadata
    {
        private readonly SourceRepository _sourceRepository;
        private readonly SearchContext _searchContext;
        private readonly int _pageSize;
        private PackageSearchResource? _packageSearchResouce;
        private RawSearchResourceV3? _rawPackageSearchResouce;
        private int? _lastPageIndex;
        private int _lastPageCount;
        private readonly PackageListCache<T> _packageListCache;
        private readonly LocalPackageSearcher<T> _localPackageSearcher;

        public ShowLatestVersionQueryContext(SourceRepository sourceRepository, string? search, bool showPreReleasePackages, int pageSize, PackageListCache<T> packageListCache)
        {
            _sourceRepository = sourceRepository;
            _searchContext = new SearchContext(search, new SearchFilter(showPreReleasePackages));
            _pageSize = pageSize;
            _packageListCache = packageListCache ?? throw new ArgumentNullException(nameof(packageListCache));
            _localPackageSearcher = new LocalPackageSearcher<T>(_searchContext);
        }

        #region IQueryContext<T> Members

        public int CurrentPage { get; private set; }

        public int BeginPackage => CurrentPage * _pageSize + (_lastPageIndex == 0 && _lastPageCount == 0 ? 0 : 1);

        public int EndPackage => CurrentPage * _pageSize + (IsLastPage ? _lastPageCount : _pageSize);

        public bool IsLastPage => CurrentPage == _lastPageIndex;

        public async Task<IList<T>> GetItemsForCurrentPage(CancellationToken token)
        {
            var packageSource = _sourceRepository.PackageSource.Source;
            List<T> list;
            if (_packageListCache.TryGetPackages(packageSource, out var packages))
            {
                list = LocalSearch(packages);
            }
            else
            {
                list = await SearchOnServer(token);

                if (list.Count > _pageSize)
                {
                    // More packages than requested, assume static feed
                    _packageListCache.SetPackages(packageSource, list);
                    list = LocalSearch(list);
                }
            }

            if (list.Count < _pageSize)
            {
                _lastPageIndex = CurrentPage;
                _lastPageCount = list.Count;
            }

            return list;
        }

        private List<T> LocalSearch(IEnumerable<T> packages)
        {
            var items = _localPackageSearcher.SearchPackages(packages);

            var list = ApplyPaging(items, _pageSize);
            return list;
        }

        private List<T> ApplyPaging(IEnumerable<T> packages, int pageSize)
        {
            var packagesList = packages.ToList();
            if (packagesList.Count > pageSize)
            {
                var skip = BeginPackage - 1;
                var count = EndPackage - skip;

                if (packagesList.Count > count + skip)
                {
                    // more packages then needed.
                    packagesList = packagesList.Skip(skip).Take(count).ToList();
                }
            }

            return packagesList;
        }

        private async Task<List<T>> SearchOnServer(CancellationToken token)
        {
            var searchText = _searchContext.SearchText;
            IEnumerable<IPackageSearchMetadata>? result = null;
            if (_searchContext.IsIdSearch)
            {
                if (!string.IsNullOrEmpty(searchText))
                {
                    var findPackageByIdResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>(token);

                    var metadata = await findPackageByIdResource.GetMetadataAsync(searchText, _searchContext.Filter.IncludePrerelease, true, NullSourceCacheContext.Instance, NullLogger.Instance, token);

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
                    var json = await _rawPackageSearchResouce.Search(searchText, _searchContext.Filter, CurrentPage * _pageSize, _pageSize, NullLogger.Instance, token);

                    result = json.Select(s => s.FromJToken<PackageSearchMetadata>());
                }

                if (result == null)
                {
                    if (_packageSearchResouce == null)
                    {
                        _packageSearchResouce = await _sourceRepository.GetResourceAsync<PackageSearchResource>(token);
                    }

                    result = await _packageSearchResouce.SearchAsync(searchText, _searchContext.Filter, CurrentPage * _pageSize, _pageSize, NullLogger.Instance, token);
                }
            }

            token.ThrowIfCancellationRequested();

            var list = result.Cast<T>().ToList();
            return list;
        }

        public bool MoveFirst()
        {
            CurrentPage = 0;
            return true;
        }

        public bool MoveNext()
        {
            if (!_lastPageIndex.HasValue || CurrentPage < _lastPageIndex)
            {
                CurrentPage++;
                return true;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (CurrentPage > 0)
            {
                CurrentPage--;
                return true;
            }
            return false;
        }

        #endregion
    }
}
