using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace NuGetPe
{
    internal class SimplePackage : IPackage
    {
        private readonly IPackageBuilder _packageBuilder;

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
            DevelopmentDependency = packageBuilder.DevelopmentDependency;
            Description = packageBuilder.Description;
            Summary = packageBuilder.Summary;
            ReleaseNotes = packageBuilder.ReleaseNotes;
            Language = packageBuilder.Language;
            Tags = packageBuilder.Tags;
            Serviceable = packageBuilder.Serviceable;
            FrameworkAssemblies = new SafeEnumerable<FrameworkAssemblyReference>(packageBuilder.FrameworkAssemblies);
            DependencySets = new SafeEnumerable<PackageDependencySet>(packageBuilder.DependencySets);
            PackageAssemblyReferences = new SafeEnumerable<PackageReferenceSet>(packageBuilder.PackageAssemblyReferences);
            Copyright = packageBuilder.Copyright;
            _packageBuilder = packageBuilder;
        }

        #region IPackage Members

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return Enumerable.Empty<IPackageAssemblyReference>(); }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return _packageBuilder.Files.Where(p => !PackageUtility.IsManifest(p.Path));
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public Stream GetStream()
        {
            Stream memoryStream = new MemoryStream();
            _packageBuilder.Save(memoryStream);
            return memoryStream;
        }

        public string Id { get; private set; }

        public TemplatebleSemanticVersion Version { get; private set; }

        public string Title { get; private set; }

        public IEnumerable<string> Authors { get; private set; }

        public IEnumerable<string> Owners { get; private set; }

        public Uri IconUrl { get; private set; }

        public Uri LicenseUrl { get; private set; }

        public DateTimeOffset? Published
        {
            get { return DateTimeOffset.Now; }
        }

        public Uri ProjectUrl { get; private set; }

        public bool RequireLicenseAcceptance { get; private set; }

        public bool DevelopmentDependency { get; private set; }

        public string Description { get; private set; }

        public string Summary { get; private set; }

        public string ReleaseNotes { get; private set; }

        public string Copyright { get; private set; }

        public string Language { get; private set; }

        public string Tags { get; private set; }

        public bool Serviceable { get; private set; }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }

        public IEnumerable<PackageDependencySet> DependencySets { get; private set; }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences { get; private set; }

        public bool IsPrerelease
        {
            get
            {
                return !String.IsNullOrEmpty(Version.SpecialVersion);
            }
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

        public bool IsAbsoluteLatestVersion
        {
            get { return true; }
        }

        public bool IsLatestVersion
        {
            get { return true; }
        }

        public DateTimeOffset LastUpdated
        {
            get { return DateTimeOffset.UtcNow; }
        }

        public long PackageSize
        {
            get { return 0; }
        }

        public string PackageHash
        {
            get { return null; }
        }

        public Version MinClientVersion
        {
            get { return null; }
        }

        #endregion

        #region Nested type: SafeEnumerable

        private class SafeEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> _source;

            public SafeEnumerable(IEnumerable<T> source)
            {
                _source = source;
            }

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return _source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        #endregion

        public void Dispose()
        {
        }
    }
}