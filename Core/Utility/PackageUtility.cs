using System;
using System.IO;

namespace NuGetPe
{
    internal static class PackageUtility
    {
        internal static bool IsManifest(string path)
        {
            return Path.GetExtension(path).Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}