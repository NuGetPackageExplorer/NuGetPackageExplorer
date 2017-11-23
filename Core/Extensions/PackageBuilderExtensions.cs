using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging;

namespace NuGetPe
{
    public static class PackageBuilderExtensions
    {
        public static IPackage Build(this PackageBuilder builder)
        {
            return new SimplePackage(builder);
        }
    }
}
