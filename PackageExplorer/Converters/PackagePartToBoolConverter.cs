using System;
using System.Windows.Data;
using PackageExplorerViewModel;

namespace PackageExplorer {
    public class PackagePartToBoolConverter : IValueConverter {

        public PackagePartToBoolConverter() {
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {

            if (value == null) {
                return null;
            }

            string type = (string)parameter;
            if (type == "file") {
                return value is PackageFile;
            }
            else if (type == "folder") {
                return value is PackageFolder;
            }
            else {
                return value is PackagePart;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
