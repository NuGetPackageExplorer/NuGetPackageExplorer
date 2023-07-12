using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
	public class FromBoolToValueConverter : IValueConverter
	{
		public object DefaultValue { get; set; }

		public object TrueValue { get; set; }

		public object FalseValue { get; set; }

		public object TrueOrDefaultValue
		{
			get => TrueValue;
			set => DefaultValue = TrueValue = value;
		}

		public object FalseOrDefaultValue
		{
			get => FalseValue;
			set => DefaultValue = FalseValue = value;
		}

		public object Convert(object value, Type targetType, object parameter, string language) => value is bool x
			? (x ? TrueValue : FalseValue)
			: DefaultValue;

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
	}
}
