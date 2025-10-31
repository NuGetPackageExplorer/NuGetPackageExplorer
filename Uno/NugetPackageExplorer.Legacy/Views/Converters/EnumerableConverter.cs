using System.Collections;

using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
    public partial class EnumerableConverter : IValueConverter
    {
        public enum ConvertMethod { StringJoin, Any, None }

        public ConvertMethod Method { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return Method switch
            {
                ConvertMethod.StringJoin => StringJoinImpl(value, targetType, parameter, language),
                ConvertMethod.Any => AnyImpl(value, targetType, parameter, language),
                ConvertMethod.None => NoneImpl(value, targetType, parameter, language),
                _ => throw new NotImplementedException($"ConvertMethod '{Method}' not implemented"),
            };
        }
        private static object StringJoinImpl(object value, Type targetType, object parameter, string language)
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
        private static object AnyImpl(object value, Type targetType, object parameter, string language)
        {
            return (value is IEnumerable enumerable && enumerable.Cast<object>().Any())
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        private static object NoneImpl(object value, Type targetType, object parameter, string language)
        {
            return (value is IEnumerable enumerable && enumerable.Cast<object>().Any())
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
