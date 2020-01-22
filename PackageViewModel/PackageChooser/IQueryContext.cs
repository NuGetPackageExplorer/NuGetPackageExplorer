using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PackageExplorerViewModel
{
    internal interface IQueryContext<T>
    {
        bool HasMore { get; }

        Task<IList<T>> LoadMore(CancellationToken token);
    }
}
