using System;
using System.IO;
using NuGetPe;

namespace PackageExplorer
{
    internal static class FileUtility
    {
        public static bool IsSupportedFile(string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return false;
            }

            var extension = Path.GetExtension(filepath);
            return Constants.ManifestExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                   Constants.PackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                   Constants.SymbolPackageExtension.Equals(extension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
