using System;
using System.Collections.Generic;
using System.Text;
using NupkgExplorer.Business.Nupkg;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NupkgExplorer.Views.Converters
{
	public class FSObjectStyleSelector : StyleSelector
	{
		public Style FileStyle { get; set; }

		public Style DirectoryStyle { get; set; }

		protected override Style SelectStyleCore(object item, DependencyObject container)
		{
			switch (item)
			{
				case NupkgContentFile _: return FileStyle;
				case NupkgContentDirectory _: return DirectoryStyle;

				default:
					this.Log().Warn($"Invalid item type: {item?.GetType().ToString() ?? "<null>"}");
					return null;
			};
		}
	}
}
