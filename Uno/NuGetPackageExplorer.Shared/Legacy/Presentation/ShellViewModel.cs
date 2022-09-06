using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NupkgExplorer.Framework.MVVM;
using NupkgExplorer.Framework.Navigation;
using NupkgExplorer.Presentation.Content;

namespace NupkgExplorer.Presentation
{
	public class ShellViewModel : ViewModelBase
	{
		public ViewModelBase ActiveContent { get => GetProperty<ViewModelBase>(); set => SetProperty(value); }

		public ShellViewModel()
		{
		}
	}
}
