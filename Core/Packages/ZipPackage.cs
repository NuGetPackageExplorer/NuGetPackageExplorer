using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Versioning;

namespace NuGetPe
{
    public class ZipPackage : IDisposable, ISignaturePackage
    {
        private const string AssemblyReferencesDir = "lib";
        private const string ResourceAssemblyExtension = ".resources.dll";
        private static readonly string[] AssemblyReferencesExtensions = new[] {".dll", ".exe", ".winmd"};

        // paths to exclude
        private static readonly string[] ExcludePaths = new[] {"_rels", "package","[Content_Types]", ".signature"};

        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private readonly Func<Stream> _streamFactory;
        private ManifestMetadata metadata;

        public ZipPackage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("Argument cannot be null.", "filePath");
            }

            if (!File.Exists(filePath))
            {
                throw new ArgumentException("File doesn't exist at '" + filePath + "'.", "filePath");
            }

            Source = filePath;
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

        public string Source { get; }

        #region IPackage Members

        public string Id
        {
            get { return metadata.Id; }
            set { metadata.Id = value; }
        }

        public NuGetVersion Version
        {
            get { return metadata.Version; }
            set { metadata.Version = value; }
        }

        public string Title
        {
            get { return metadata.Title; }
            set { metadata.Title = value; }
        }

        public IEnumerable<string> Authors
        {
            get { return metadata.Authors; }
            set { metadata.Authors = value; }
        }

        public IEnumerable<string> Owners
        {
            get { return metadata.Owners; }
            set { metadata.Owners = value; }
        }

        public Uri IconUrl
        {
            get { return metadata.IconUrl; }
            set { metadata.SetIconUrl(value?.ToString()); }
        }

        public Uri LicenseUrl
        {
            get { return metadata.LicenseUrl; }
            set { metadata.SetLicenseUrl(value?.ToString()); }
        }

        public Uri ProjectUrl
        {
            get { return metadata.ProjectUrl; }
            set { metadata.SetProjectUrl(value?.ToString()); }
        }

        public bool RequireLicenseAcceptance
        {
            get { return metadata.RequireLicenseAcceptance; }
            set { metadata.RequireLicenseAcceptance = value; }
        }

        public bool DevelopmentDependency
        {
            get { return metadata.DevelopmentDependency; }
            set { metadata.DevelopmentDependency = value; }
        }

        public string Description
        {
            get { return metadata.Description; }
            set { metadata.Description = value; }
        }

        public string Summary
        {
            get { return metadata.Summary; }
            set { metadata.Summary = value; }
        }

        public string ReleaseNotes
        {
            get { return metadata.ReleaseNotes; }
            set { metadata.ReleaseNotes = value; }
        }

        public string Language
        {
            get { return metadata.Language; }
            set { metadata.Language = value; }
        }

        public string Tags
        {
            // Ensure tags start and end with an empty " " so we can do contains filtering reliably
            get { return !string.IsNullOrWhiteSpace(metadata.Tags) ? $" {metadata.Tags} " : metadata.Tags; }
            set { metadata.Tags = value?.Trim(); }
        }

        public bool Serviceable
        {
            get { return metadata.Serviceable; }
            set { metadata.Serviceable = value; }
        }

        public string Copyright
        {
            get { return metadata.Copyright; }
            set { metadata.Copyright = value; }
        }

        public Version MinClientVersion
        {
            get { return metadata.MinClientVersion; }
            set { metadata.MinClientVersionString = value?.ToString(); }
        }

        public IEnumerable<PackageDependencyGroup> DependencyGroups
        {
            get { return metadata.DependencyGroups; }
            set { metadata.DependencyGroups = value; }
        }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return metadata.PackageAssemblyReferences; }
            set { metadata.PackageAssemblyReferences = value; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences
        {
            get { return metadata.FrameworkReferences; }
            set { metadata.FrameworkReferences = value; }
        }

        public IEnumerable<ManifestContentFiles> ContentFiles
        {
            get { return metadata.ContentFiles; }
            set { metadata.ContentFiles = value; }
        }

        public IEnumerable<PackageType> PackageTypes
        {
            get { return metadata.PackageTypes; }
            set { metadata.PackageTypes = value; }
        }

        public RepositoryMetadata Repository
        {
            get { return metadata.Repository; }
            set { metadata.Repository = value; }
        }

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
            get { return -1; }
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
                    _lastUpdated = File.GetLastWriteTimeUtc(Source);
                }
                return _lastUpdated.Value;
            }
        }

        public bool IsPrerelease
        {
            get
            {
                return Version.IsPrerelease;
            }
        }

        public bool IsSigned { get; private set; }

        public bool IsVerified => false;

        public SignatureInfo PublisherSignature { get; private set; }

        public SignatureInfo RepositorySignature { get; private set; }

        public VerifySignaturesResult VerificationResult { get; private set; }


        // Keep a list of open stream here, and close on dispose.
        private List<IDisposable> _danglingStreams = new List<IDisposable>();

        public IEnumerable<IPackageFile> GetFiles()
        {
            var stream = _streamFactory();
            var reader = new MyPackageArchiveReader(stream, false); // should not close
           
            _danglingStreams.Add(reader);           // clean up on dispose


            var entries = reader.GetZipEntries();
            return (from entry in entries
                    where IsPackageFile(entry)
                    select new ZipPackageFile(reader, entry)).ToList();
        }

        public Stream GetStream()
        {
            return _streamFactory();
        }

        #endregion

        public async Task LoadSignatureDataAsync()
        {
            using (var reader = new PackageArchiveReader(_streamFactory(), false))
            {
                IsSigned = await reader.IsSignedAsync(CancellationToken.None);
                if (IsSigned)
                {
                    try
                    {
                        // Load signature data
                        // TODO: Repo + Author sig?
                        var sig = await reader.GetSignatureAsync(CancellationToken.None);
                    
                        // There will only be one
                        if (sig.Type == SignatureType.Author)
                        {
                            PublisherSignature = new SignatureInfo(sig);
                        } else if (sig.Type == SignatureType.Repository)
                        {
                            RepositorySignature = new SignatureInfo(sig);
                        }
                    }
                    catch (SignatureException)
                    {
                    }
                    
                }
            }
        }

        public async Task VerifySignatureAsync()
        {
            using (var reader = new PackageArchiveReader(_streamFactory(), false))
            {
                var signed = await reader.IsSignedAsync(CancellationToken.None);
                if (signed)
                {
                    // Check verification 
                    var trustProviders = SignatureVerificationProviderFactory.GetSignatureVerificationProviders();
                    var verifier = new PackageSignatureVerifier(trustProviders, SignedPackageVerifierSettings.VerifyCommandDefaultPolicy);

                    VerificationResult = await verifier.VerifySignaturesAsync(reader, CancellationToken.None);
                }
            }
        }

        private void EnsureManifest()
        {
            using (var stream = _streamFactory())
            using (var reader = new PackageArchiveReader(stream))
            {
                var manifest = Manifest.ReadFrom(reader.GetNuspec(), false);
                metadata = manifest.Metadata;
            }
        }

        private static bool IsPackageFile(ZipArchiveEntry entry)
        {
            // We exclude any opc files and the manifest file (.nuspec)
            var path = entry.FullName;
            
            return !path.EndsWith("/") && !ExcludePaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) &&
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


        private class MyPackageArchiveReader : PackageArchiveReader
        {
            private ZipArchive zipArchive;

           /// <summary>Nupkg package reader</summary>
            /// <param name="stream">Nupkg data stream.</param>
            /// <param name="leaveStreamOpen">If true the nupkg stream will not be closed by the zip reader.</param>
            public MyPackageArchiveReader(Stream stream, bool leaveStreamOpen) : base(stream, leaveStreamOpen)
            {
                zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            }
            public ReadOnlyCollection<ZipArchiveEntry> GetZipEntries() => zipArchive.Entries;
        }
    }
}
