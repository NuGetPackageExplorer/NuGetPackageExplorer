using System;

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
    }
}
