using Microsoft.UI.Xaml.Data;

namespace NupkgExplorer.Views.Converters
{
    public partial class FromObjectToValueConverter : IValueConverter
    {
        public object? NullValue { get; set; }

        public object? NotNullValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language) => (value == null || value == DependencyProperty.UnsetValue)
            ? NullValue!
            : NotNullValue!;

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException("Only one-way conversion is supported.");
    }
}
