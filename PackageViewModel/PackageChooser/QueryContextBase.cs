using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Threading.Tasks;

namespace PackageExplorerViewModel
{
    internal abstract class QueryContextBase<T>
    {
        private int? _totalItemCount;

        public int TotalItemCount 
        {
            get
            {
                return _totalItemCount ?? 0;
            }
        }

        protected bool TotalItemCountReady
        {
            get
            {
                return _totalItemCount.HasValue;
            }
        }

        public IQueryable<T> Source { get; private set; }

        protected QueryContextBase(IQueryable<T> source)
        {
            Source = source;
        }

        protected async Task<IEnumerable<T>> LoadData(IQueryable<T> query)
        {
            var dataServiceQuery = query as DataServiceQuery<T>;
            if (dataServiceQuery != null)
            {
                dataServiceQuery = dataServiceQuery.AddQueryOption("semVerLevel", "2.0.0");
                var queryResponse = (QueryOperationResponse<T>)
                    await Task.Factory.FromAsync<IEnumerable<T>>(dataServiceQuery.BeginExecute(null, null), dataServiceQuery.EndExecute);
                
                try
                {
                    _totalItemCount = (int)queryResponse.TotalCount;
                }
                catch (InvalidOperationException)
                {
                    if (!TotalItemCountReady)
                    {
                        // the server doesn't return $inlinecount value,
                        // fall back to using $count query
                        _totalItemCount = Source.Count();
                    }
                }

                return queryResponse;
            }
            else
            {
                if (!TotalItemCountReady)
                {
                    _totalItemCount = Source.Count();
                }

                return query;
            }
        }
    }
}