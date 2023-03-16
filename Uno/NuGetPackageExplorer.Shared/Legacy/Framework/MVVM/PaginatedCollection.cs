﻿using System;
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
using Windows.UI.Xaml.Data;

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
			return Task.Run<LoadMoreItemsResult>(async () =>
			{
				var items = await _fetch(_start, _pageSize);
				await CoreApplication.MainView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
				{
					foreach (var item in items)
					{
						Add(item);
					}
				});

				_start += items.Length;

				return new LoadMoreItemsResult() { Count = (uint)items.Length };
			}).AsAsyncOperation();
		}

		public bool HasMoreItems => true;
	}
}
