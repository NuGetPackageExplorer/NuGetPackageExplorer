using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PackageExplorer {
    public class CountToVisibilityConverter : IValueConverter {

        public bool Inverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            double count = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);

            double threshold = 0;
            if (parameter != null) {
                threshold = System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture);
            }

            Visibility returnValue = count > threshold ? Visibility.Visible : Visibility.Collapsed;

            if (Inverted) {
                if (returnValue == Visibility.Visible) {
                    returnValue = Visibility.Collapsed;
                }
                else {
                    returnValue = Visibility.Visible;
                }
            }

            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}