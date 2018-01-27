using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PackageExplorerViewModel
{
    internal interface IQueryContext<T>
    {
        int CurrentPage { get; }

        Task<IList<T>> GetItemsForCurrentPage(CancellationToken token);

        bool MoveFirst();
        bool MoveNext();
        bool MovePrevious();
    }
}
