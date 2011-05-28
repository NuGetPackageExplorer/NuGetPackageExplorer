using System;
using System.Collections.Generic;

namespace PackageExplorerViewModel {
    internal interface IQueryContext<T> {
        int BeginPackage { get; }
        int EndPackage { get; }
        int TotalItemCount { get; }

        IEnumerable<T> GetItemsForCurrentPage();

        bool MoveFirst();
        bool MoveLast();
        bool MoveNext();
        bool MovePrevious();
    }
}