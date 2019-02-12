using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Data;

namespace PackageExplorer
{
    public class CertificateToSubjectConverter : IValueConverter
    {
        #region IValueConverter Members

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var cert = (X509Certificate2)value;
            if (cert == null)
            {
                return null;
            }

            var dict = DistinguishedNameParser.Parse(cert.Subject);
            string? cn = null;
            if (dict.TryGetValue("CN", out var cns))
            {
                // get the CN. it may be quoted
                cn = string.Join("+", cns.Select(s => s.Replace("\"", "")));
            }

            return cn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
