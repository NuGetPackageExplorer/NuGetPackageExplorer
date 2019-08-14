using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel.PackageSearch
{
    internal class LocalPackageSearcher<T> where T : IPackageSearchMetadata
    {
        private readonly SearchContext _searchContext;

        public LocalPackageSearcher(SearchContext searchContext)
        {
            _searchContext = searchContext;
        }

        public IEnumerable<T> SearchPackages(IEnumerable<T> packages)
        {
            var searchText = _searchContext.SearchText;
            var searchFilter = _searchContext.Filter;

            if (!searchFilter.IncludePrerelease)
            {
                packages = packages.Where(p => p is PackageSearchMetadata meta && !meta.Version.IsPrerelease);
            }

            if (_searchContext.IsIdSearch)
            {
                packages = packages.Where(p => string.Equals(p.Identity.Id, searchText, StringComparison.OrdinalIgnoreCase));
            }
            else if (!string.IsNullOrEmpty(searchText))
            {
                // Support multiple terms
                var searchValues = SplitValues(searchText, " ");

                packages = packages
                    .Select(p => new { score = searchValues.Sum(s => CalcScore(p, s)), package = p })
                    .Where(it => it.score > 0) // first filtering before sorting for performance reasons
                    .OrderByDescending(it => it.score)
                    .Select(it => it.package);
            }

            return packages;
        }

        /// <summary>
        /// Calculate score for package with search term. 0 means no match
        /// </summary>
        /// <returns>number, 0 or higher</returns>
        private static int CalcScore(T package, string searchText)
        {
            var score = ScoreForEquals(package.Identity.Id, searchText);

            score += ScoreForContains(package.Identity.Id, searchText);
            score += ScoreForContains(package.Title, searchText);
            score += ScoreForContains(package.Summary, searchText);
            score += ScoreForContains(package.Description, searchText);

            var tags = SplitValues(package.Tags, " ");
            score += ScoreForContains(tags, searchText);

            var authors = SplitValues(package.Authors, ",");
            score += ScoreForContains(authors, searchText);

            if (score > 0)
            {
                // boost scope if there is a match
                score += CalcDownloadScore(package.DownloadCount);
            }

            return score;
        }

        private static int CalcDownloadScore(long? packageDownloadCount)
        {
            if (packageDownloadCount >= 0)
            {
                // From: NuGet.Services.Metadata
                // This score ranges from 0 to less than 100, assuming that the most downloaded
                // package has less than 500 million downloads. This scoring function increases
                // quickly at first and then becomes approximately linear near the upper bound.
                var downloadScore = (int)Math.Sqrt(packageDownloadCount.Value) / 220;
                return downloadScore;
            }

            return 0;
        }

        private static int ScoreForContains(IEnumerable<string> values, string searchText)
        {
            return values.Sum(t => ScoreForContains(t, searchText));
        }

        private static IEnumerable<string> SplitValues(string value, string separator)
        {
            if (value == null)
            {
                return new List<string>();
            }
            var values = value.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
            return values;
        }

        private static int ScoreForContains(string text, string searchText)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            return text.Contains(searchText, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }

        private static int ScoreForEquals(string text, string searchText)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }
            return text.Equals(searchText, StringComparison.OrdinalIgnoreCase) ? 5 : 0;
        }
    }
}
