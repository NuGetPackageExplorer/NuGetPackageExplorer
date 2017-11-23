using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageFileExtensions
    {
        public static string OriginalPath(this IPackageFile packageFile)
        {
            return (packageFile as PhysicalPackageFile)?.SourcePath;
        }
    }
}
