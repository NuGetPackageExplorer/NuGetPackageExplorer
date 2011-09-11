using System;
using System.IO;
using NuGet;

namespace PackageExplorer {
    internal static class FileUtility {
        public static bool IsSupportedFile(string filepath) {
            if (String.IsNullOrEmpty(filepath)) {
                return false;
            }

            string extension = Path.GetExtension(filepath);
            return extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
