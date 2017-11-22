using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGetPe.Resources;

namespace NuGetPe
{
    public class ZipPackage : IPackage, IDisposable
    {
        private const string AssemblyReferencesDir = "lib";
        private const string ResourceAssemblyExtension = ".resources.dll";
        private static readonly string[] AssemblyReferencesExtensions = new[] {".dll", ".exe", ".winmd"};

        // paths to exclude
        private static readonly string[] ExcludePaths = new[] {"_rels", "package","[Content_Types]"};

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
            _streamFactory = () =>
            {
                try
                {
                    return File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                catch (UnauthorizedAccessException)
                {
                    //just try read
                    return File.Open(filePath, FileMode.Open,FileAccess.Read);
                }

            };
            EnsureManifest();
        }

        public string Source
        {
            get { return _filePath; }
        }

        #region IPackage Members

        public string Id { get; set; }

        public TemplatebleSemanticVersion Version { get; set; }

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
                using(var reader = new PackageArchiveReader(stream))
                {
                    return (from file in reader.GetFiles()
                            where IsAssemblyReference(file)
                            select new ZipPackageAssemblyReference(reader, file)).ToList();
                }
            }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; set; }

        // Keep a list of open stream here, and close on dispose.
        private List<IDisposable> _danglingStreams = new List<IDisposable>();

        public IEnumerable<IPackageFile> GetFiles()
        {
            Stream stream = _streamFactory();
            var reader = new PackageArchiveReader(stream, false); // should not close
           
            _danglingStreams.Add(reader);           // clean up on dispose

            
            return (from file in reader.GetFiles()
                    where IsPackageFile(file)
                    select new ZipPackageFile(reader, file)).ToList();
        }

        public Stream GetStream()
        {
            return _streamFactory();
        }

        #endregion

        private void EnsureManifest()
        {
            using (Stream stream = _streamFactory())
            using (var reader = new PackageArchiveReader(stream))
            {
                var nuspec = reader.NuspecReader;
                
                Id = nuspec.GetId();
                Version = new TemplatebleSemanticVersion(nuspec.GetVersion());
                Title = nuspec.GetTitle();
                Authors = nuspec.GetAuthors().Split(',');
                Owners = nuspec.GetOwners().Split(',');

                var iconUrl = nuspec.GetIconUrl();
                IconUrl = string.IsNullOrWhiteSpace(iconUrl) ? null : new Uri(iconUrl);

                var licenseUrl = nuspec.GetLicenseUrl();
                LicenseUrl = string.IsNullOrWhiteSpace(licenseUrl) ? null : new Uri(licenseUrl);

                var projectUrl = nuspec.GetProjectUrl();
                ProjectUrl = string.IsNullOrWhiteSpace(projectUrl) ? null : new Uri(projectUrl);

                RequireLicenseAcceptance = nuspec.GetRequireLicenseAcceptance();
                Description = nuspec.GetDescription();
                Summary = nuspec.GetSummary();
                ReleaseNotes = nuspec.GetReleaseNotes();
                Copyright = nuspec.GetCopyright();
                Language = nuspec.GetLanguage();
                Tags = nuspec.GetTags();
                Serviceable = reader.IsServiceable();
                DependencySets = (from g in nuspec.GetDependencyGroups()
                                  select new PackageDependencySet(g.TargetFramework.IsAny ? null : g.TargetFramework, g.Packages))
                                  .ToList();
                FrameworkAssemblies = (from g in nuspec.GetFrameworkReferenceGroups()
                                      from item in g.Items
                                      group g.TargetFramework by item into grp
                                      select new FrameworkAssemblyReference(grp.Key, grp))
                                      .ToList();
                PackageAssemblyReferences = (from g in nuspec.GetReferenceGroups()
                                             select new PackageReferenceSet(g.TargetFramework.IsAny ? null : g.TargetFramework, g.Items))
                                             .ToList();
                Published = File.GetLastWriteTimeUtc(_filePath);
                var nv = nuspec.GetMinClientVersion();
                MinClientVersion = nv != null ? new Version(nv.Major, nv.Minor) : null; 
                DevelopmentDependency = nuspec.GetDevelopmentDependency();

                // Ensure tags start and end with an empty " " so we can do contains filtering reliably
                if (!String.IsNullOrEmpty(Tags))
                {
                    Tags = " " + Tags + " ";
                }

            }
        }

        private static bool IsAssemblyReference(string path)
        {
            // Assembly references are in lib/ and have a .dll/.exe extension
            return path.StartsWith(AssemblyReferencesDir, StringComparison.OrdinalIgnoreCase) &&
                   // Exclude resource assemblies
                   !path.EndsWith(ResourceAssemblyExtension, StringComparison.OrdinalIgnoreCase) &&
                   AssemblyReferencesExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsPackageFile(string path)
        {
            // We exclude any opc files and the manifest file (.nuspec)
            return !ExcludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
                   !PackageUtility.IsManifest(path);
        }

        public override string ToString()
        {
            return this.GetFullName();
        }

        public void Dispose()
        {
            _danglingStreams.ForEach(ds => ds.Dispose());
        }
    }
}