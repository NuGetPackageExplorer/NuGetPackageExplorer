using System;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Data;

namespace PackageExplorer
{
    public class CertificateToSubjectConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cert = (X509Certificate2) value;

            var subect = cert?.SubjectName.Name;
            return subect;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}