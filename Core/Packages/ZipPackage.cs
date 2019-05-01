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
        // paths to exclude
        private static readonly string[] ExcludePaths = new[] { "_rels/", "package/", "[Content_Types].xml", ".signature.p7s" };

        // We don't store the steam itself, just a way to open the stream on demand
        // so we don't have to hold on to that resource
        private readonly Func<Stream> _streamFactory;
        private ManifestMetadata _metadata;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public ZipPackage(string filePath)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
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
                    return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }

            };
            EnsureManifest();
        }

        public string Source { get; }

        public string Id
        {
            get { return _metadata.Id; }
            set { _metadata.Id = value; }
        }

        public NuGetVersion Version
        {
            get { return _metadata.Version; }
            set { _metadata.Version = value; }
        }

        public string? Title
        {
            get { return _metadata.Title; }
            set { _metadata.Title = value; }
        }

        public IEnumerable<string> Authors
        {
            get { return _metadata.Authors; }
            set { _metadata.Authors = value; }
        }

        public IEnumerable<string> Owners
        {
            get { return _metadata.Owners; }
            set { _metadata.Owners = value; }
        }

        public Uri? IconUrl
        {
            get { return _metadata.IconUrl; }
            set { _metadata.SetIconUrl(value?.ToString()); }
        }

        public Uri? LicenseUrl
        {
            get { return _metadata.LicenseUrl; }
            set { _metadata.SetLicenseUrl(value?.ToString()); }
        }

        public Uri? ProjectUrl
        {
            get { return _metadata.ProjectUrl; }
            set { _metadata.SetProjectUrl(value?.ToString()); }
        }

        public bool RequireLicenseAcceptance
        {
            get { return _metadata.RequireLicenseAcceptance; }
            set { _metadata.RequireLicenseAcceptance = value; }
        }

        public bool DevelopmentDependency
        {
            get { return _metadata.DevelopmentDependency; }
            set { _metadata.DevelopmentDependency = value; }
        }

        public string? Description
        {
            get { return _metadata.Description; }
            set { _metadata.Description = value; }
        }

        public string? Summary
        {
            get { return _metadata.Summary; }
            set { _metadata.Summary = value; }
        }

        public string? ReleaseNotes
        {
            get { return _metadata.ReleaseNotes; }
            set { _metadata.ReleaseNotes = value; }
        }

        public string? Language
        {
            get { return _metadata.Language; }
            set { _metadata.Language = value; }
        }

        public string? Tags
        {
            // Ensure tags start and end with an empty " " so we can do contains filtering reliably
            get { return !string.IsNullOrWhiteSpace(_metadata.Tags) ? $" {_metadata.Tags} " : _metadata.Tags; }
            set { _metadata.Tags = value?.Trim(); }
        }

        public bool Serviceable
        {
            get { return _metadata.Serviceable; }
            set { _metadata.Serviceable = value; }
        }

        public string? Copyright
        {
            get { return _metadata.Copyright; }
            set { _metadata.Copyright = value; }
        }

        public Version MinClientVersion
        {
            get { return _metadata.MinClientVersion; }
            set { _metadata.MinClientVersionString = value?.ToString(); }
        }

        public IEnumerable<PackageDependencyGroup> DependencyGroups
        {
            get { return _metadata.DependencyGroups; }
            set { _metadata.DependencyGroups = value; }
        }

        public IEnumerable<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return _metadata.PackageAssemblyReferences; }
            set { _metadata.PackageAssemblyReferences = value; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkReferences
        {
            get { return _metadata.FrameworkReferences; }
            set { _metadata.FrameworkReferences = value; }
        }

        public IEnumerable<ManifestContentFiles> ContentFiles
        {
            get { return _metadata.ContentFiles; }
            set { _metadata.ContentFiles = value; }
        }

        public IEnumerable<PackageType> PackageTypes
        {
            get { return _metadata.PackageTypes; }
            set { _metadata.PackageTypes = value; }
        }

        public RepositoryMetadata? Repository
        {
            get { return _metadata.Repository; }
            set { _metadata.Repository = value; }
        }

        public LicenseMetadata? LicenseMetadata
        {
            get { return _metadata.LicenseMetadata; }
            set { _metadata.LicenseMetadata = value; }
        }

        public IEnumerable<FrameworkReferenceGroup> FrameworkReferenceGroups
        {
            get => _metadata.FrameworkReferenceGroups;
            set => _metadata.FrameworkReferenceGroups = value;
        }

        public DateTimeOffset? Published
        {
            get;
            set;
        }

        public Uri? ReportAbuseUrl
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

        public SignatureInfo? PublisherSignature { get; private set; }

        public RepositorySignatureInfo? RepositorySignature { get; private set; }

        public VerifySignaturesResult VerificationResult { get; private set; }


        // Keep a list of open stream here, and close on dispose.
        private readonly List<IDisposable> _danglingStreams = new List<IDisposable>();

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
        
        public async Task LoadSignatureDataAsync()
        {
            using var reader = new PackageArchiveReader(_streamFactory(), false);
            IsSigned = await reader.IsSignedAsync(CancellationToken.None);
            if (IsSigned)
            {
                try
                {
                    var sig = await reader.GetPrimarySignatureAsync(CancellationToken.None);

                    // Author signatures must be the primary, but they can contain
                    // a repository counter signature
                    if (sig.Type == SignatureType.Author)
                    {
                        PublisherSignature = new SignatureInfo(sig);

                        var counter = RepositoryCountersignature.GetRepositoryCountersignature(sig);
                        if (counter != null)
                        {
                            RepositorySignature = new RepositorySignatureInfo(counter);
                        }
                    }
                    else if (sig.Type == SignatureType.Repository)
                    {
                        RepositorySignature = new RepositorySignatureInfo(sig);
                    }
                }
                catch (SignatureException)
                {
                }

            }
        }

        public async Task VerifySignatureAsync()
        {
            using var reader = new PackageArchiveReader(_streamFactory(), false);
            var signed = await reader.IsSignedAsync(CancellationToken.None);
            if (signed)
            {
                // Check verification

                var trustProviders = new ISignatureVerificationProvider[]
                {
                        new IntegrityVerificationProvider(),
                        new SignatureTrustAndValidityVerificationProvider()
                };
                var verifier = new PackageSignatureVerifier(trustProviders);

                VerificationResult = await verifier.VerifySignaturesAsync(reader, SignedPackageVerifierSettings.GetVerifyCommandDefaultPolicy(), CancellationToken.None);
            }
        }

        private void EnsureManifest()
        {
            using var stream = _streamFactory();
            using var reader = new PackageArchiveReader(stream);
            var manifest = Manifest.ReadFrom(ManifestUtility.ReadManifest(reader.GetNuspec()), false);
            _metadata = manifest.Metadata;
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
            private readonly ZipArchive _zipArchive;

            /// <summary>Nupkg package reader</summary>
            /// <param name="stream">Nupkg data stream.</param>
            /// <param name="leaveStreamOpen">If true the nupkg stream will not be closed by the zip reader.</param>
            public MyPackageArchiveReader(Stream stream, bool leaveStreamOpen) : base(stream, leaveStreamOpen)
            {
                _zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
            }
            public ReadOnlyCollection<ZipArchiveEntry> GetZipEntries() => _zipArchive.Entries;
        }
    }
}
