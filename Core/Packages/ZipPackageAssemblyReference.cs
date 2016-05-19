using System;
using System.Diagnostics;
using System.IO.Packaging;
using System.Text;

namespace NuGetPe
{
    internal class ZipPackageAssemblyReference : ZipPackageFile, IPackageAssemblyReference
    {
        public ZipPackageAssemblyReference(PackagePart part)
            : base(part)
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