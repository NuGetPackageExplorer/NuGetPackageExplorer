using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public static class PackageExtensions {

        public static IEnumerable<string> GetFilesInFolders(this IPackage package, string folder) {
            if (folder == null) {
                throw new ArgumentNullException("folder");
            }

            if (String.IsNullOrEmpty(folder)) {
                // return files at the root
                return from s in package.GetFiles()
                       where s.Path.IndexOf(Path.DirectorySeparatorChar) == -1
                       select s.Path;
            }
            else {
                string prefix = folder + Path.DirectorySeparatorChar;
                return from s in package.GetFiles()
                       where s.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                       select s.Path.Substring(prefix.Length);
            }
        }

        public static IEnumerable<string> GetFilesUnderRoot(this IPackage package) {
            return GetFilesInFolders(package, String.Empty);
        }
    }
}