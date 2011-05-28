using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Packaging;
using System.Runtime.Versioning;
using System.Text;

namespace NuGet {
    internal class ZipPackageAssemblyReference : ZipPackageFile, IPackageAssemblyReference {
        private FrameworkName _targetFramework;

        public ZipPackageAssemblyReference(PackagePart part)
            : base(part) {
            Debug.Assert(Path.StartsWith("lib", StringComparison.OrdinalIgnoreCase), "path doesn't start with lib");

            // Get rid of the lib folder            
            string path = Path.Substring(3).Trim(System.IO.Path.DirectorySeparatorChar);

            _targetFramework = VersionUtility.ParseFrameworkFolderName(path);
        }

        public FrameworkName TargetFramework {
            get {
                return _targetFramework;
            }
        }

        IEnumerable<FrameworkName> IFrameworkTargetable.SupportedFrameworks {
            get {
                if (TargetFramework != null) {
                    yield return TargetFramework;
                }
                yield break;
            }
        }

        public string Name {
            get {
                return System.IO.Path.GetFileName(Path);
            }
        }

        public override string ToString() {
            var builder = new StringBuilder();
            if (TargetFramework != null) {
                builder.Append(TargetFramework).Append(" ");
            }
            builder.Append(Name).AppendFormat(" ({0})", Path);
            return builder.ToString();
        }
    }
}
