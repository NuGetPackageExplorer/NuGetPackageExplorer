using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.MVVM;

using PackageExplorer;

using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Framework.Navigation
{
    [Export]
	public class DialogService
	{
		private readonly IDictionary<Type, Type> _mapping = new Dictionary<Type, Type>();

		public DialogService()
		{
		}

		public void Register<TContentDialog, TViewModel>()
			where TContentDialog : ContentDialog, new()
			where TViewModel : ViewModelBase
		{
			_mapping.Add(typeof(TViewModel), typeof(TContentDialog));
		}

		public Task<ContentDialogResult> ShowAsync<TViewModel>(CancellationToken ct) 
			where TViewModel : ViewModelBase, new()
		{
            return ShowAsync(ct, App.Current.Container.GetExportedValue<TViewModel>()!);
		}

		public async Task<ContentDialogResult> ShowAsync<TViewModel>(CancellationToken ct, TViewModel viewModel) 
			where TViewModel : ViewModelBase
		{
			if (!_mapping.TryGetValue(typeof(TViewModel), out var dialogType))
			{
				throw new InvalidOperationException($"There is no dialog associated with '{typeof(TViewModel).Name}'.");
			}

            //todo: check&switch thread
            var dialog = (ContentDialog)App.Current.Container.GetExportedValue(dialogType);
            dialog.XamlRoot = App.Current.MainWindow?.Content?.XamlRoot;

            dialog.DataContext = viewModel;

			using (ct.Register(CloseDialog))
			{
				return await dialog.ShowAsync();
			}

			void CloseDialog() => _ = dialog.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () => dialog.Hide());
		}
	}
}
