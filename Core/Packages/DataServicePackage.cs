using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Data.Services.Common;
using System.IO;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    [DataServiceKey("Id", "Version")]
    [EntityPropertyMapping("LastUpdated", SyndicationItemProperty.Updated, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Id", SyndicationItemProperty.Title, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Authors", SyndicationItemProperty.AuthorName, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    [EntityPropertyMapping("Summary", SyndicationItemProperty.Summary, SyndicationTextContentKind.Plaintext, keepInContent: false)]
    public class DataServicePackage : IPackage {
        public DataServicePackage() {
        }

        public string Id {
            get;
            set;
        }

        public string Version {
            get;
            set;
        }

        public string Title {
            get {
                return CorePackage == null ? null : CorePackage.Title;
            }
        }

        public string Authors {
            get;
            set;
        }

        public bool IsLatestVersion {
            get;
            set;
        }

        public IEnumerable<string> Owners {
            get {
                return CorePackage == null ? null : CorePackage.Owners;
            }
        }

        public Uri IconUrl {
            get {
                return CorePackage == null ? null : CorePackage.IconUrl;
            }
        }

        public Uri LicenseUrl {
            get {
                return CorePackage == null ? null : CorePackage.LicenseUrl;
            }
        }

        public Uri ProjectUrl {
            get {
                return CorePackage == null ? null : CorePackage.ProjectUrl;
            }
        }

        public Uri ReportAbuseUrl {
            get {
                return CorePackage == null ? null : CorePackage.ReportAbuseUrl;
            }
        }

        public DateTimeOffset LastUpdated {
            get;
            set;
        }        

        public int DownloadCount {
            get;
            set;
        }

        public int VersionDownloadCount {
            get;
            set;
        }

        public double Rating {
            get;
            set;
        }

        public double VersionRating {
            get;
            set;
        }

        public int RatingsCount {
            get;
            set;
        }

        public bool RequireLicenseAcceptance {
            get {
                return CorePackage == null ? false : CorePackage.RequireLicenseAcceptance;
            }
        }

        public string Description {
            get {
                return CorePackage == null ? null : CorePackage.Description;
            }
        }

        public string Summary {
            get {
                return CorePackage == null ? null : CorePackage.Summary;
            }
        }

        public string Language {
            get {
                return CorePackage == null ? null : CorePackage.Language;
            }
        }

        public string Tags {
            get {
                return CorePackage == null ? null : CorePackage.Tags;
            }
        }

        public IEnumerable<PackageDependency> Dependencies {
            get {
                return CorePackage == null ? Enumerable.Empty<PackageDependency>() : CorePackage.Dependencies;
            }
        }

        public string PackageHash {
            get;
            set;
        }

        public IPackage CorePackage { 
            get; 
            set; 
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                return CorePackage == null ? Enumerable.Empty<string>() : CorePackage.Authors;
            }
        }

        Version IPackageMetadata.Version {
            get {
                if (Version != null) {
                    return new Version(Version);
                }
                return null;
            }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
            get {
                return CorePackage.AssemblyReferences;
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
            get {
                return CorePackage.FrameworkAssemblies;
            }
        }

        public IEnumerable<IPackageFile> GetFiles() {
            return CorePackage.GetFiles();
        }

        public Stream GetStream() {
            return CorePackage.GetStream();
        }

        public override string ToString() {
            return this.GetFullName();
        }
    }
}