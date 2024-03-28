using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.ApplicationModel.Core;
using Microsoft.UI.Xaml;
using CommunityToolkit.WinUI.UI.Controls;

namespace NupkgExplorer.Views.Extensions
{
	public static class DataGridExtensions
	{
		#region Property: DelayedItemsSource

		public static DependencyProperty DelayedItemsSourceProperty { get; } = DependencyProperty.RegisterAttached(
			"DelayedItemsSource",
			typeof(IEnumerable),
			typeof(DataGridExtensions),
			new PropertyMetadata(default, (d, e) => d.Maybe<DataGrid>(control => OnDelayedItemsSourceChanged(control, e))));

		public static IEnumerable GetDelayedItemsSource(DataGrid obj) => (IEnumerable)obj.GetValue(DelayedItemsSourceProperty);
		public static void SetDelayedItemsSource(DataGrid obj, IEnumerable value) => obj.SetValue(DelayedItemsSourceProperty, value);

		#endregion

		private static void OnDelayedItemsSourceChanged(DataGrid sender, DependencyPropertyChangedEventArgs e)
		{
			// DataGrid not showing rows with initial values, only "late" provided values are displayed somehow...
			// This is the workaround for that:
			_ = RunOnUIThread(() => sender.ItemsSource = (IEnumerable)e.NewValue);
		}

		private static async Task RunOnUIThread(Action action)
		{
			await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
			{
				action();
			});
		}
	}
}
