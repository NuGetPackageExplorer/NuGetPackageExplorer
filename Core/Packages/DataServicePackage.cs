using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;

namespace NuGet
{
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext,
        keepInContent: false)]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext,
        keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext,
        keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext,
        keepInContent: false)]
    public class DataServicePackage : IPackage
    {
        public string Version { get; set; }
        public string Authors { get; set; }
        public bool IsLatestVersion { get; set; }
        public bool IsAbsoluteLatestVersion { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
        public long PackageSize { get; set; }
        public string PackageHash { get; set; }
        public DateTimeOffset? Published { get; set; }
        public bool IsPrerelease { get; set; }

        public IPackage CorePackage { get; set; }

        #region IPackage Members

        public string Id { get; set; }

        public string Title
        {
            get { return CorePackage == null ? null : CorePackage.Title; }
        }

        public IEnumerable<string> Owners
        {
            get { return CorePackage == null ? null : CorePackage.Owners; }
        }

        public Uri IconUrl
        {
            get { return CorePackage == null ? null : CorePackage.IconUrl; }
        }

        public Uri LicenseUrl
        {
            get { return CorePackage == null ? null : CorePackage.LicenseUrl; }
        }

        public Uri ProjectUrl
        {
            get { return CorePackage == null ? null : CorePackage.ProjectUrl; }
        }

        public Uri ReportAbuseUrl
        {
            get { return CorePackage == null ? null : CorePackage.ReportAbuseUrl; }
        }

        public int DownloadCount { get; set; }

        public int VersionDownloadCount { get; set; }

        public bool RequireLicenseAcceptance
        {
            get { return CorePackage != null && CorePackage.RequireLicenseAcceptance; }
        }

        public bool DevelopmentDependency
        {
            get { return CorePackage != null && CorePackage.DevelopmentDependency; }
        }

        public string Description
        {
            get { return CorePackage == null ? null : CorePackage.Description; }
        }

        public string Summary
        {
            get { return CorePackage == null ? null : CorePackage.Summary; }
        }

        public string ReleaseNotes
        {
            get { return CorePackage == null ? null : CorePackage.ReleaseNotes; }
        }

        public string Copyright
        {
            get { return CorePackage == null ? null : CorePackage.Copyright; }
        }

        public string Language
        {
            get { return CorePackage == null ? null : CorePackage.Language; }
        }

        public string Tags
        {
            get { return CorePackage == null ? null : CorePackage.Tags; }
        }

        public bool Serviceable
        {
            get { return CorePackage != null && CorePackage.Serviceable; }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get
            {
                return CorePackage == null ? Enumerable.Empty<PackageDependencySet>() : CorePackage.DependencySets;
            }
        }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences
        {
            get
            {
                return CorePackage == null ? Enumerable.Empty<PackageReferenceSet>() : CorePackage.PackageAssemblyReferences;
            }
        }

        IEnumerable<string> IPackageMetadata.Authors
        {
            get { return CorePackage == null ? Enumerable.Empty<string>() : CorePackage.Authors; }
        }

        SemanticVersion IPackageMetadata.Version
        {
            get
            {
                if (Version != null)
                {
                    return new SemanticVersion(Version);
                }
                return null;
            }
        }

        public Version MinClientVersion
        {
            get;
            set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return CorePackage.AssemblyReferences; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { return CorePackage.FrameworkAssemblies; }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return CorePackage.GetFiles();
        }

        public Stream GetStream()
        {
            return CorePackage.GetStream();
        }

        #endregion

        public override string ToString()
        {
            return this.GetFullName();
        }
    }
}