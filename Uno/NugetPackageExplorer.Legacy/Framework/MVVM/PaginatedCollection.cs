using System.Collections.ObjectModel;

using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;

using Uno.Extensions;
using Uno.Logging;

using Windows.Foundation;

namespace NupkgExplorer.Framework.MVVM
{
    public class PaginatedCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
    {
        private readonly Lazy<ILogger> _logger = new Lazy<ILogger>(typeof(PaginatedCollection<T>).Log);

        public delegate Task<T[]> Fetch(int start, int count);

        private readonly Fetch _fetch;
        private int _start;
        private readonly int _pageSize;
        private readonly TaskCompletionSource _initialized;

        public PaginatedCollection(Fetch fetch, int pageSize, TaskCompletionSource initialized)
        {
            _fetch = fetch;
            _start = 0;
            _pageSize = pageSize;
            _initialized = initialized;
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var dq = DispatcherQueue.GetForCurrentThread();

            return Task.Run(async () =>
            {
                var items = await _fetch(_start, _pageSize);

                var tcs = new TaskCompletionSource<bool>();
                dq.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    foreach (var item in items)
                    {
                        Add(item);
                    }

                    tcs.TrySetResult(true);
                });

                await tcs.Task;

                _start += items.Length;
                _initialized.TrySetResult();

                return new LoadMoreItemsResult() { Count = (uint)items.Length };
            }).AsAsyncOperation();
        }

        public bool HasMoreItems => true;
    }
}
