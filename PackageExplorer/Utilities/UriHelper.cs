using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace PackageExplorer
{
    internal static class UriHelper
    {
        public static void OpenExternalLink(Uri licenseUrl)
        {
            if (IsRemoteUri(licenseUrl))
            {
                Process.Start(licenseUrl.AbsoluteUri);
            }
        }

        public static bool IsRemoteUri(this Uri url)
        {
            if (url == null)
            {
                return false;
            }

            // mitigate security risk
            if (url.IsFile || url.IsLoopback || url.IsUnc)
            {
                return false;
            }

            var scheme = url.Scheme;
            return (scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                    scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }

        public static Dictionary<string, string> GetRequestParameters(this Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            var matches = Regex.Matches(uri.Query, @"[\?&](([^&=]+)=([^&=#]*))");
            return matches.Cast<Match>().ToDictionary(
                m => Uri.UnescapeDataString(m.Groups[2].Value),
                m => Uri.UnescapeDataString(m.Groups[3].Value),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}