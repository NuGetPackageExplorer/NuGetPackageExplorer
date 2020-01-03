using System;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel.PackageSearch
{
    public class SearchContext
    {
        public string? SearchText { get; }

        public SearchFilter Filter { get; }

        public bool IsIdSearch { get; }

        public SearchContext(string? searchText, SearchFilter filter)
        {
            if (searchText?.StartsWith("id:", StringComparison.OrdinalIgnoreCase) == true)
            {
                IsIdSearch = true;
                SearchText = searchText.Substring(3).Trim();
            }
            else
            {
                SearchText = searchText;
            }

            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }
    }
}
