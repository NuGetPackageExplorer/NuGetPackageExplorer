using System;
using System.Collections.Generic;
using System.Text;
using NupkgExplorer.Business.Nupkg.Files;
using Uno.Extensions;
using Uno.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace NupkgExplorer.Views.Converters
{
	public class FileContentTemplateSelector : DataTemplateSelector
	{
		public DataTemplate TextContentTemplate { get; set; }
		public DataTemplate ImageContentTemplate { get; set; }
		public DataTemplate BinaryContentTemplate { get; set; }
		public DataTemplate AssemblyContentTemplate { get; set; }

		protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) => SelectTemplateCore(item);

		protected override DataTemplate SelectTemplateCore(object item)
		{
			if (item is TextFileContent) return TextContentTemplate;
			if (item is ImageFileContent) return ImageContentTemplate;
			if (item is AssemblyFileContent) return AssemblyContentTemplate;

			return null;
		}
	}
}
