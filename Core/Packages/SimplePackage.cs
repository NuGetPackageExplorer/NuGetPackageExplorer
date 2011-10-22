using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet
{
    internal class SimplePackage : IPackage
    {
        private IPackageBuilder _packageBuilder;

        public SimplePackage(IPackageBuilder packageBuilder)
        {
            if (packageBuilder == null)
            {
                throw new ArgumentNullException("packageBuilder");
            }

            Id = packageBuilder.Id;
            Version = packageBuilder.Version;
            Title = packageBuilder.Title;
            Authors = new SafeEnumerable<string>(packageBuilder.Authors);
            Owners = new SafeEnumerable<string>(packageBuilder.Owners);
            IconUrl = packageBuilder.IconUrl;
            LicenseUrl = packageBuilder.LicenseUrl;
            ProjectUrl = packageBuilder.ProjectUrl;
            RequireLicenseAcceptance = packageBuilder.RequireLicenseAcceptance;
            Description = packageBuilder.Description;
            Summary = packageBuilder.Summary;
            ReleaseNotes = packageBuilder.ReleaseNotes;
            Language = packageBuilder.Language;
            Tags = packageBuilder.Tags;
            FrameworkAssemblies = new SafeEnumerable<FrameworkAssemblyReference>(packageBuilder.FrameworkAssemblies);
            Dependencies = new SafeEnumerable<PackageDependency>(packageBuilder.Dependencies);
            References = new SafeEnumerable<AssemblyReference>(packageBuilder.References);
            Copyright = packageBuilder.Copyright;
            _packageBuilder = packageBuilder;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get {
                return Enumerable.Empty<IPackageAssemblyReference>();
            }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return _packageBuilder.Files.Where(p => !PackageUtility.IsManifest(p.Path));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Stream GetStream()
        {
            Stream memoryStream = new MemoryStream();
            _packageBuilder.Save(memoryStream);
            return memoryStream;
        }

        public string Id
        {
            get;
            private set;
        }

        public SemanticVersion Version
        {
            get;
            private set;
        }

        public string Title
        {
            get;
            private set;
        }

        public IEnumerable<string> Authors
        {
            get;
            private set;
        }

        public IEnumerable<string> Owners
        {
            get;
            private set;
        }

        public Uri IconUrl
        {
            get;
            private set;
        }

        public Uri LicenseUrl
        {
            get;
            private set;
        }

        public Uri ProjectUrl
        {
            get;
            private set;
        }

        public bool RequireLicenseAcceptance
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public string Summary
        {
            get;
            private set;
        }

        public string ReleaseNotes {
            get;
            private set;
        }

        public string Copyright {
            get;
            private set;
        }

        public string Language
        {
            get;
            private set;
        }

        public string Tags
        {
            get;
            private set;
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get;
            private set;
        }

        public IEnumerable<PackageDependency> Dependencies
        {
            get;
            private set;
        }

        public IEnumerable<AssemblyReference> References {
            get;
            private set;
        }

        public Uri ReportAbuseUrl
        {
            get { return null; }
        }

        public int DownloadCount
        {
            get { return -1; }
        }

        public int VersionDownloadCount
        {
            get { return -1; }
        }

        private class SafeEnumerable<T> : IEnumerable<T> {
            private readonly IEnumerable<T> _source;
            public SafeEnumerable(IEnumerable<T> source) {
                _source = source;
            }

            public IEnumerator<T> GetEnumerator() {
                return _source.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }
    }
}