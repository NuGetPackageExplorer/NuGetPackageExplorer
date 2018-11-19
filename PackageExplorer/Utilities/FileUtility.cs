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
            return extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(Constants.SymbolPackageExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
