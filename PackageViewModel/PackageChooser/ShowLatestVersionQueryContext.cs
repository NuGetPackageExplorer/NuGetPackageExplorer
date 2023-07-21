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
        private readonly PackageListCache<T> _packageListCache;
        private readonly LocalPackageSearcher<T> _localPackageSearcher;
        private PackageSearchResource? _packageSearchResource;

#pragma warning disable CS0618 // Type or member is obsolete
        private RawSearchResourceV3? _rawPackageSearchResource;
#pragma warning restore CS0618 // Type or member is obsolete

        private int? _lastPageIndex;

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

        public bool HasMore => CurrentPage != _lastPageIndex;

        public async Task<IList<T>> LoadMore(CancellationToken token)
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
            }
            else
            {
                CurrentPage++;
            }

            return list;
        }

        private List<T> LocalSearch(IEnumerable<T> packages)
        {
            var items = _localPackageSearcher.SearchPackages(packages);

            return items.Skip(CurrentPage * _pageSize).Take(_pageSize).ToList();
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
                if (_packageSearchResource == null && _rawPackageSearchResource == null)
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    _rawPackageSearchResource = await _sourceRepository.GetResourceAsync<RawSearchResourceV3>(token);
#pragma warning restore CS0618 // Type or member is obsolete
                }

                if (_rawPackageSearchResource != null)
                {
                    // Don't run the cpu-bound operations on GUI thread
                    result = await Task.Run(async () =>
                    {
#pragma warning disable CS0618 // Type or member is obsolete
                        var json = await _rawPackageSearchResource.Search(searchText, _searchContext.Filter, CurrentPage * _pageSize, _pageSize, NullLogger.Instance, token);
#pragma warning restore CS0618 // Type or member is obsolete
                        return json.Select(s => s.FromJToken<PackageSearchMetadata>()).ToList();
                    }, token);
                }

                if (result == null)
                {
                    if (_packageSearchResource == null)
                    {
                        _packageSearchResource = await _sourceRepository.GetResourceAsync<PackageSearchResource>(token);
                    }

                    result = await _packageSearchResource.SearchAsync(searchText, _searchContext.Filter, CurrentPage * _pageSize, _pageSize, NullLogger.Instance, token);
                }
            }

            token.ThrowIfCancellationRequested();

            var list = result.Cast<T>().ToList();
            return list!;
        }

        #endregion
    }
}
