using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PackageExplorer {
    internal static class UriHelper {
        public static void OpenExternalLink(Uri licenseUrl) {
            if (licenseUrl == null) {
                return;
            }

            // mitigate security risk
            if (licenseUrl.IsFile || licenseUrl.IsLoopback || licenseUrl.IsUnc) {
                return;
            }

            string scheme = licenseUrl.Scheme;
            if (scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) {
                // REVIEW: Will this allow a package author to execute arbitrary program on user's machine?
                // We have limited the url to be HTTP only, but is it sufficient?
                System.Diagnostics.Process.Start(licenseUrl.AbsoluteUri);
            }
        }

        public static Dictionary<string, string> GetRequestParameters(Uri uri) {
            var matches = Regex.Matches(uri.Query, @"[\?&](([^&=]+)=([^&=#]*))");
            return matches.Cast<Match>().ToDictionary(
                m => Uri.UnescapeDataString(m.Groups[2].Value),
                m => Uri.UnescapeDataString(m.Groups[3].Value),
                StringComparer.InvariantCultureIgnoreCase
            );
        }
    }
}