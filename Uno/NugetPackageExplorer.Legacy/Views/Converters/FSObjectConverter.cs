using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NupkgExplorer.Business.Nupkg;
using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
	public class FSObjectConverter : IValueConverter
	{
		public string FileValue { get; set; }
		public string DirectoryValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			switch (value)
			{
				case NupkgContentFile _: return FileValue;
				case NupkgContentDirectory _: return DirectoryValue;

				default: return null;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
	}
}
