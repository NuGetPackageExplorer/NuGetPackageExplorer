using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
	public class EnumerableConverter : IValueConverter
	{
		public enum ConvertMethod { StringJoin, Any, None }

		public ConvertMethod Method { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			switch (Method)
			{
				case ConvertMethod.StringJoin: return StringJoinImpl(value, targetType, parameter, language);
				case ConvertMethod.Any: return AnyImpl(value, targetType, parameter, language);
				case ConvertMethod.None: return NoneImpl(value, targetType, parameter, language);

				default: throw new NotImplementedException($"ConvertMethod '{Method}' not implemented");
			}
		}
		private object StringJoinImpl(object value, Type targetType, object parameter, string language)
		{
			if (value is IEnumerable enumerable)
			{
				return string.Join(parameter?.ToString(), enumerable.Cast<object>());
			}
			else
			{
				return value;
			}
		}
		private object AnyImpl(object value, Type targetType, object parameter, string language)
		{
			return (value is IEnumerable enumerable && enumerable.Cast<object>().Any())
				? Visibility.Visible
				: Visibility.Collapsed;
		}
		private object NoneImpl(object value, Type targetType, object parameter, string language)
		{
			return (value is IEnumerable enumerable && enumerable.Cast<object>().Any())
				? Visibility.Collapsed
				: Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
	}
}
