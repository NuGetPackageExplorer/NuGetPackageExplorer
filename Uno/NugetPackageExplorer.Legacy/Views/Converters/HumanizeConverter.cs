using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
	// inspired from https://github.com/Humanizr/Humanizer
	public class HumanizeConverter : IValueConverter
	{
		public enum ConversionType { FileSize, LargeNumber }

		public ConversionType Conversion { get; set; }

		private static readonly string[] FileSizeUnits = "B,KB,MB,GB,TB".Split(',');
		private static readonly string[] LargeNumberUnits = ",K,M,B,T".Split(',');

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			switch (Conversion)
			{
				case ConversionType.FileSize: return HumanizeFileSize(value, targetType, parameter, language);
				case ConversionType.LargeNumber: return HumanizeLargeNumber(value, targetType, parameter, language);

				default: throw new NotImplementedException($"ConversionType '{Conversion}' not implemented");
			}
		}

		private string HumanizeFileSize(object value, Type targetType, object parameter, string language)
		{
			if (value is long l)
			{
				var log2 = Math.Log(l, 2);
				var tier = Math.Max(0, Math.Min((int)(Math.Abs(log2 / 10)), FileSizeUnits.Length - 1));
				var unit = FileSizeUnits[tier];
				var format = tier == 0 ? "0" : "0.0";

				return $"{(l / Math.Pow(2, 10 * tier)).ToString(format)} {unit}";
			}

			return null;
		}

		private string HumanizeLargeNumber(object value, Type targetType, object parameter, string language)
		{
			if (value is long l)
			{
				var log2 = Math.Log10(l);
				var tier = Math.Max(0, Math.Min((int)(Math.Abs(log2 / 3)), FileSizeUnits.Length - 1));
				var unit = LargeNumberUnits[tier];
				var format = tier == 0 ? "0" : "0.0";

				return (l / Math.Pow(10, 3 * tier)).ToString(format) + unit;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
	}
}
