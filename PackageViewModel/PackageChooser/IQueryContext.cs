using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PackageExplorerViewModel
{
    internal interface IQueryContext<T>
    {
        int BeginPackage { get; }
        int EndPackage { get; }
        int TotalItemCount { get; }

        Task<IList<T>> GetItemsForCurrentPage(CancellationToken token);

        bool MoveFirst();
        bool MoveLast();
        bool MoveNext();
        bool MovePrevious();
    }
}