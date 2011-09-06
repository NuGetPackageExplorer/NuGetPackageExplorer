using System;
using System.Windows.Data;
using PackageExplorerViewModel;
using System.Windows;

namespace PackageExplorer {
    public class PackagePartToVisibilityConverter : IValueConverter {

        public PackagePartToVisibilityConverter() {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

            if (value == null) {
                return null;
            }

            string type = (string)parameter;
            if (type == "file") {
                return BoolToVisibility(value is PackageFile);
            }
            else if (type == "folder") {
                return BoolToVisibility(value is PackageFolder);
            }
            else {
                return BoolToVisibility(value is PackagePart);
            }
        }

        private Visibility BoolToVisibility(bool b) {
            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
