using System;
using System.Diagnostics;
using System.IO.Packaging;
using System.Text;
using NuGet.Packaging;

namespace NuGetPe
{
    internal class ZipPackageAssemblyReference : ZipPackageFile
    {
        public ZipPackageAssemblyReference(PackageArchiveReader reader, string path)
            : base(reader, path)
        {
            Debug.Assert(Path.StartsWith("lib", StringComparison.OrdinalIgnoreCase), "path doesn't start with lib");
        }

        public string Name
        {
            get { return System.IO.Path.GetFileName(Path); }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            if (TargetFramework != null)
            {
                builder.Append(TargetFramework).Append(" ");
            }
            builder.Append(Name).AppendFormat(" ({0})", Path);
            return builder.ToString();
        }
    }
}