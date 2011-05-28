using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel {
    public class EmptyPackage : IPackage {
        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            return Enumerable.Empty<IPackageFile>();
        }

        public System.IO.Stream GetStream() {
            return null;
        }

        public string Id {
            get { return "MyPackage"; }
        }

        public Version Version {
            get { return new Version("1.0"); }
        }

        public string Title {
            get { return String.Empty; }
        }

        public IEnumerable<string> Authors {
            get { yield return Environment.UserName; }
        }

        public IEnumerable<string> Owners {
            get { return Enumerable.Empty<string>(); }
        }

        public Uri IconUrl {
            get { return null; }
        }

        public Uri LicenseUrl {
            get { return null; }
        }

        public Uri ProjectUrl {
            get { return null; }
        }

        public bool RequireLicenseAcceptance {
            get { return false; }
        }

        public string Description {
            get { return "My package description."; }
        }

        public string Summary {
            get { return String.Empty; }
        }

        public string Language {
            get { return null; }
        }

        public string Tags {
            get { return null; }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get { return Enumerable.Empty<PackageDependency>(); }
        }

        public Uri ReportAbuseUrl {
            get { return null; }
        }

        public int DownloadCount {
            get { return -1; }
        }

        public int VersionDownloadCount {
            get { return -1; }
        }

        public int RatingsCount {
            get { return -1; }
        }

        public double Rating {
            get { return -1; }
        }

        public double VersionRating {
            get { return -1; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
            get { return Enumerable.Empty<FrameworkAssemblyReference>(); }
        }
    }
}