using NuGet.Packaging;
using System;
using System.Collections.Generic;
using System.Data.Services.Common;
using System.IO;
using System.Linq;
using NuGet.Versioning;
using NuGet.Packaging.Core;
using System.Security.Cryptography.X509Certificates;

namespace NuGetPe
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

        public IEnumerable<PackageDependencyGroup> DependencyGroups
        {
            get
            {
                return CorePackage == null ? Enumerable.Empty<PackageDependencyGroup>() : CorePackage.DependencyGroups;
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

        public Version MinClientVersion
        {
            get { return CorePackage?.MinClientVersion; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences
        {
            get { return CorePackage?.FrameworkReferences ?? Enumerable.Empty<FrameworkAssemblyReference>(); }
        }

        public IEnumerable<ManifestContentFiles> ContentFiles => CorePackage?.ContentFiles ?? Enumerable.Empty<ManifestContentFiles>();

        public IEnumerable<PackageType> PackageTypes => CorePackage?.PackageTypes ?? Enumerable.Empty<PackageType>();

        public RepositoryMetadata Repository => CorePackage?.Repository;

        NuGetVersion IPackageMetadata.Version => NuGetVersion.Parse(Version);

        public bool IsSigned => CorePackage?.IsSigned ?? false;

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

        public void Dispose()
        {
        }
    }
}