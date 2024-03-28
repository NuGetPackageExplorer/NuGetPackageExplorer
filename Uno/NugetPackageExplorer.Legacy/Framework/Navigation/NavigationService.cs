using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;
using NupkgExplorer.Framework.MVVM;

using PackageExplorer;

using Uno.Extensions;
using Uno.Logging;

using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Framework.Navigation
{
    [Export]
	public class NavigationService
	{
		public event EventHandler<(Type PageType, ViewModelBase ViewModel)> Navigated;

		private readonly IDictionary<Type, Type> _mapping = new Dictionary<Type, Type>();

		public NavigationService()
		{
		}

		public void Register<TPage, TViewModel>()
			where TPage: Page, new()
			where TViewModel: ViewModelBase
		{
			_mapping.Add(typeof(TViewModel), typeof(TPage));
		}


		public void NavigateTo<TViewModel>()
			where TViewModel : ViewModelBase, new()
        {
            NavigateToCore(new TViewModel());
        }

		public void NavigateTo<TViewModel>(TViewModel viewModel)
			where TViewModel: ViewModelBase
        {
            NavigateToCore(viewModel);
        }

        public void NavigateTo(ViewModelBase viewModel) => NavigateToCore(viewModel);

        private void NavigateToCore(ViewModelBase viewModel)
        {
            try
            {
                if (!_mapping.TryGetValue(viewModel.GetType(), out var pageType))
                {
                    throw new InvalidOperationException($"There is no page associated with '{viewModel.GetType().Name}'.");
                }
            
                App.Current.Container.SatisfyImportsOnce(viewModel);
            
                Navigated?.Invoke(this, (pageType, viewModel));
            }
            catch (Exception e)
            {
                this.Log().Error($"Navigation failed for `{viewModel?.GetType().Name ?? "<null>"}`:", e);
                throw;
            }
        }
	}
}
