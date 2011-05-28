using System;
using System.IO.Packaging;
using System.IO;

namespace NuGet {    
    internal static class UriUtility {
        /// <summary>
        /// Converts a uri to a path. Only used for local paths.
        /// </summary>
        internal static string GetPath(Uri uri) {
            string path = uri.OriginalString;
            if (path.StartsWith("/", StringComparison.Ordinal)) {
                path = path.Substring(1);
            }

            // Bug 483: We need the unescaped uri string to ensure that all characters are valid for a path.
            // Change the direction of the slashes to match the filesystem.
            return Uri.UnescapeDataString(path.Replace('/', Path.DirectorySeparatorChar));
        }

        internal static Uri CreatePartUri(string path) {
            return PackUriHelper.CreatePartUri(new Uri(Uri.EscapeDataString(path), UriKind.Relative));
        }
    }
}
