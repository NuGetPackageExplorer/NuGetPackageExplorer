using System;
using System.IO;

namespace NuGetPe
{
    internal static class PackageUtility
    {
        internal static bool IsManifest(string path)
        {
            return Constants.ManifestExtension.Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        }
    }
}
