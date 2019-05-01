using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGetPe;

namespace PackageExplorerViewModel
{
    public class EmptyPackage : IPackage
    {
        #region IPackage Members

        public IEnumerable<IPackageFile> GetFiles()
        {
            return Enumerable.Empty<IPackageFile>();
        }

        public IEnumerable<FrameworkReferenceGroup> FrameworkReferenceGroups => Enumerable.Empty<FrameworkReferenceGroup>();

        public Stream GetStream()
        {
            return new MemoryStream();
        }

        public string Id
        {
            get { return "MyPackage"; }
        }

        public NuGetVersion Version
        {
            get { return NuGetVersion.Parse("1.0.0"); }
        }

        public string? Title
        {
            get { return string.Empty; }
        }

        public IEnumerable<string> Authors
        {
            get { yield return Environment.UserName; }
        }

        public IEnumerable<string> Owners
        {
            get { return Enumerable.Empty<string>(); }
        }

        public Uri? IconUrl
        {
            get { return null; }
        }

        public Uri? LicenseUrl
        {
            get { return null; }
        }

        public Uri? ProjectUrl
        {
            get { return null; }
        }

        public bool RequireLicenseAcceptance
        {
            get { return false; }
        }

        public bool DevelopmentDependency
        {
            get { return false; }
        }

        public string? Description
        {
            get { return "My package description."; }
        }

        public string? Summary
        {
            get { return null; }
        }

        public string? ReleaseNotes
        {
            get { return null; }
        }

        public string? Copyright
        {
            get { return null; }
        }

        public string? Language
        {
            get { return null; }
        }

        public string? Tags
        {
            get { return null; }
        }

        public bool Serviceable
        {
            get { return false; }
        }

        public IEnumerable<PackageDependencyGroup> DependencySets
        {
            get { return Enumerable.Empty<PackageDependencyGroup>(); }
        }

        public Uri? ReportAbuseUrl
        {
            get { return null; }
        }

        public int DownloadCount
        {
            get { return -1; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { return Enumerable.Empty<FrameworkAssemblyReference>(); }
        }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return Enumerable.Empty<PackageReferenceSet>(); }
        }

        public bool IsAbsoluteLatestVersion
        {
            get { return false; }
        }

        public bool IsLatestVersion
        {
            get { return false; }
        }

        public bool IsPrerelease
        {
            get { return false; }
        }

        public DateTimeOffset LastUpdated
        {
            get { return DateTimeOffset.MinValue; }
        }

        public DateTimeOffset? Published
        {
            get { return DateTimeOffset.Now; }
        }

        public Version? MinClientVersion
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences => Enumerable.Empty<FrameworkAssemblyReference>();

        public IEnumerable<PackageDependencyGroup> DependencyGroups => Enumerable.Empty<PackageDependencyGroup>();

        public IEnumerable<ManifestContentFiles> ContentFiles => Enumerable.Empty<ManifestContentFiles>();

        public IEnumerable<PackageType> PackageTypes => Enumerable.Empty<PackageType>();

        public RepositoryMetadata? Repository => null;

        public LicenseMetadata? LicenseMetadata => null;

        #endregion

        public void Dispose()
        {
        }
    }
}
