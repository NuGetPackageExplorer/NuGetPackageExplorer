using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Uno.Extensions;
using Uno.Logging;

using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
    public class StringFormatConverter : IValueConverter
    {
        public enum FormattingCulture { CurrentCulture, InvariantCulture }

        public FormattingCulture Culture { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (parameter is not string format)
            {
                this.Log().ErrorIfEnabled(() => $"Invalid parameter: {(parameter is null ? "<null>" : $"[{parameter.GetType().Name}]{parameter}")}");
                return value;
            }

            var culture = Culture switch
            {
                FormattingCulture.CurrentCulture => CultureInfo.CurrentCulture,
                FormattingCulture.InvariantCulture => CultureInfo.InvariantCulture,

                _ => throw new NotImplementedException($"FormattingCulture '{Culture}' not implemented"),
            };
            return string.Format(culture, format, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
