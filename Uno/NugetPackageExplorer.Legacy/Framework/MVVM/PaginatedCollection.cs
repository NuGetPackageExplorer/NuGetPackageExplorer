using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Dispatching;

namespace NupkgExplorer.Framework.MVVM
{
	public class PaginatedCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
	{
		private readonly Lazy<ILogger> _logger = new Lazy<ILogger>(typeof(PaginatedCollection<T>).Log);

		public delegate Task<T[]> Fetch(int start, int count);

		private readonly Fetch _fetch;
		private int _start, _pageSize;

		public PaginatedCollection(Fetch fetch, int pageSize)
		{
			_fetch = fetch;
			_start = 0;
			_pageSize = pageSize;
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

				return new LoadMoreItemsResult() { Count = (uint)items.Length };
			}).AsAsyncOperation();
		}

		public bool HasMoreItems => true;
	}
}
