using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using NuGetPe.Resources;

namespace NuGetPe
{
    public class ZipPackage : IPackage
    {
        private const string AssemblyReferencesDir = "lib";
        private const string ResourceAssemblyExtension = ".resources.dll";
        private static readonly string[] AssemblyReferencesExtensions = new[] {".dll", ".exe", ".winmd"};

        // paths to exclude
        private static readonly string[] ExcludePaths = new[] {"_rels", "package"};

        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private readonly Func<Stream> _streamFactory;
        private readonly string _filePath;

        public ZipPackage(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Argument cannot be null.", "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException("File doesn't exist at '" + filePath + "'.", "filePath");
            }

            _filePath = filePath;
            _streamFactory = () => File.Open(filePath,FileMode.Open,FileAccess.ReadWrite,FileShare.ReadWrite);
            EnsureManifest();
        }

        public string Source
        {
            get { return _filePath; }
        }

        #region IPackage Members

        public string Id { get; set; }

        public NuGet.SemanticVersion Version { get; set; }

        public string Title { get; set; }

        public IEnumerable<string> Authors { get; set; }

        public IEnumerable<string> Owners { get; set; }

        public Uri IconUrl { get; set; }

        public Uri LicenseUrl { get; set; }

        public Uri ProjectUrl { get; set; }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public Uri ReportAbuseUrl
        {
            get { return null; }
        }

        public int DownloadCount
        {
            get { return 0; }
        }

        public int VersionDownloadCount
        {
            get { return 0; }
        }

        public bool RequireLicenseAcceptance { get; set; }

        public bool DevelopmentDependency { get; set; }

        public string Description { get; set; }

        public string Summary { get; set; }

        public string ReleaseNotes { get; set; }

        public string Language { get; set; }

        public string Tags { get; set; }

        public bool Serviceable { get; set; }

        public string Copyright { get; set; }

        public Version MinClientVersion
        {
            get;
            private set;
        }

        public bool IsAbsoluteLatestVersion
        {
            get { return true; }
        }

        public bool IsLatestVersion
        {
            get { return this.IsReleaseVersion(); }
        }

        private DateTimeOffset? _lastUpdated;
        public DateTimeOffset LastUpdated
        {
            get 
            {
                if (_lastUpdated == null)
                {
                    _lastUpdated = File.GetLastWriteTimeUtc(_filePath);
                }
                return _lastUpdated.Value;
            }
        }

        private long? _packageSize;
        public long PackageSize
        {
            get 
            {
                if (_packageSize == null)
                {
                    _packageSize = new FileInfo(_filePath).Length;
                }
                return _packageSize.Value;
            }
        }

        public string PackageHash
        {
            get { return null; }
        }

        public bool IsPrerelease
        {
            get
            {
                return !String.IsNullOrEmpty(Version.SpecialVersion);
            }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get;
            set;
        }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences
        {
            get;
            private set;
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get
            {
                using (Stream stream = _streamFactory())
                {
                    Package package = Package.Open(stream);
                    return (from part in package.GetParts()
                            where IsAssemblyReference(part)
                            select new ZipPackageAssemblyReference(part)).ToList();
                }
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; set; }

        public IEnumerable<IPackageFile> GetFiles()
        {
            Package package = Package.Open(_streamFactory()); // should not close

            return (from part in package.GetParts()
                    where IsPackageFile(part)
                    select new ZipPackageFile(part)).ToList();
        }

        public Stream GetStream()
        {
            return _streamFactory();
        }

        #endregion

        private void EnsureManifest()
        {
            using (Stream stream = _streamFactory())
            {
                Package package = Package.Open(stream);

                PackageRelationship relationshipType =
                    package.GetRelationshipsByType(Constants.PackageRelationshipNamespace +
                                                   PackageBuilder.ManifestRelationType).SingleOrDefault();

                if (relationshipType == null)
                {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                PackagePart manifestPart = package.GetPart(relationshipType.TargetUri);

                if (manifestPart == null)
                {
                    throw new InvalidOperationException(NuGetResources.PackageDoesNotContainManifest);
                }

                using (Stream manifestStream = manifestPart.GetStream())
                {
                    Manifest manifest = Manifest.ReadFrom(manifestStream);
                    IPackageMetadata metadata = manifest.Metadata;

                    Id = metadata.Id;
                    Version = metadata.Version;
                    Title = metadata.Title;
                    Authors = metadata.Authors;
                    Owners = metadata.Owners;
                    IconUrl = metadata.IconUrl;
                    LicenseUrl = metadata.LicenseUrl;
                    ProjectUrl = metadata.ProjectUrl;
                    RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
                    Description = metadata.Description;
                    Summary = metadata.Summary;
                    ReleaseNotes = metadata.ReleaseNotes;
                    Copyright = metadata.Copyright;
                    Language = metadata.Language;
                    Tags = metadata.Tags;
                    Serviceable = metadata.Serviceable;
                    DependencySets = metadata.DependencySets;
                    FrameworkAssemblies = metadata.FrameworkAssemblies;
                    PackageAssemblyReferences = metadata.PackageAssemblyReferences;
                    Published = File.GetLastWriteTimeUtc(_filePath);
                    MinClientVersion = metadata.MinClientVersion;
                    DevelopmentDependency = metadata.DevelopmentDependency;

                    // Ensure tags start and end with an empty " " so we can do contains filtering reliably
                    if (!String.IsNullOrEmpty(Tags))
                    {
                        Tags = " " + Tags + " ";
                    }
                }
            }
        }

        private static bool IsAssemblyReference(PackagePart part)
        {
            // Assembly references are in lib/ and have a .dll/.exe extension
            string path = UriUtility.GetPath(part.Uri);
            return path.StartsWith(AssemblyReferencesDir, StringComparison.OrdinalIgnoreCase) &&
                   // Exclude resource assemblies
                   !path.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                   AssemblyReferencesExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPackageFile(PackagePart part)
        {
            string path = UriUtility.GetPath(part.Uri);
            // We exclude any opc files and the manifest file (.nuspec)
            return !ExcludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !PackageUtility.IsManifest(path);
        }

        public override string ToString()
        {
            return this.GetFullName();
        }
    }
}