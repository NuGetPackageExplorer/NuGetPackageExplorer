using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using NuGet.Resources;

namespace NuGet {
    public sealed class PackageBuilder : IPackageBuilder {
        private const string DefaultContentType = "application/octet";
        internal const string ManifestRelationType = "manifest";

        public PackageBuilder(string path)
            : this(path, Path.GetDirectoryName(path)) {

        }

        public PackageBuilder(string path, string basePath)
            : this() {
            using (Stream stream = File.OpenRead(path)) {
                ReadManifest(stream, basePath);
            }
        }

        public PackageBuilder(Stream stream, string basePath)
            : this() {
            ReadManifest(stream, basePath);
        }

        public PackageBuilder() {
            Files = new Collection<IPackageFile>();
            Dependencies = new Collection<PackageDependency>();
            FrameworkReferences = new Collection<FrameworkAssemblyReference>();
            Authors = new HashSet<string>();
            Owners = new HashSet<string>();
            Tags = new HashSet<string>();
        }

        public string Id {
            get;
            set;
        }

        public Version Version {
            get;
            set;
        }

        public string Title {
            get;
            set;
        }

        public ISet<string> Authors {
            get;
            private set;
        }

        public ISet<string> Owners {
            get;
            private set;
        }

        public Uri IconUrl {
            get;
            set;
        }

        public Uri LicenseUrl {
            get;
            set;
        }

        public Uri ProjectUrl {
            get;
            set;
        }

        public bool RequireLicenseAcceptance {
            get;
            set;
        }

        public string Description {
            get;
            set;
        }

        public string Summary {
            get;
            set;
        }

        public string Language {
            get;
            set;
        }

        public ISet<string> Tags {
            get;
            private set;
        }

        public Collection<PackageDependency> Dependencies {
            get;
            private set;
        }

        public Collection<IPackageFile> Files {
            get;
            private set;
        }

        public Collection<FrameworkAssemblyReference> FrameworkReferences {
            get;
            private set;
        }

        IEnumerable<string> IPackageMetadata.Authors {
            get {
                return Authors;
            }
        }

        IEnumerable<string> IPackageMetadata.Owners {
            get {
                return Owners;
            }
        }

        string IPackageMetadata.Tags {
            get {
                return String.Join(" ", Tags);
            }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies {
            get {
                return Dependencies;
            }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies {
            get {
                return FrameworkReferences;
            }
        }

        public void Save(Stream stream) {
            // Make sure we're saving a valid package id
            PackageIdValidator.ValidatePackageId(Id);

            // Throw if the package doesn't contain any dependencies nor content
            if (!Files.Any() && !Dependencies.Any() && !FrameworkReferences.Any()) {
                throw new InvalidOperationException(NuGetResources.CannotCreateEmptyPackage);
            }

            using (Package package = Package.Open(stream, FileMode.Create)) {
                // Validate and write the manifest
                WriteManifest(package);

                // Write the files to the package
                WriteFiles(package);

                // Copy the metadata properties back to the package
                package.PackageProperties.Creator = String.Join(",", Authors);
                package.PackageProperties.Description = Description;
                package.PackageProperties.Identifier = Id;
                package.PackageProperties.Version = Version.ToString();
                package.PackageProperties.Language = Language;
                package.PackageProperties.Keywords = ((IPackageMetadata)this).Tags;
            }
        }

        private void ReadManifest(Stream stream, string basePath) {
            // Deserialize the document and extract the metadata
            Manifest manifest = Manifest.ReadFrom(stream);
            IPackageMetadata metadata = manifest.Metadata;

            Id = metadata.Id;
            Version = metadata.Version;
            Title = metadata.Title;
            Authors.AddRange(metadata.Authors);
            Owners.AddRange(metadata.Owners);
            IconUrl = metadata.IconUrl;
            LicenseUrl = metadata.LicenseUrl;
            ProjectUrl = metadata.ProjectUrl;
            RequireLicenseAcceptance = metadata.RequireLicenseAcceptance;
            Description = metadata.Description;
            Summary = metadata.Summary;
            Language = metadata.Language;

            if (metadata.Tags != null) {
                Tags.AddRange(ParseTags(metadata.Tags));
            }

            Dependencies.AddRange(metadata.Dependencies);
            FrameworkReferences.AddRange(metadata.FrameworkAssemblies);
            
            // If there's no base path then ignore the files node
            if (basePath != null) {
                if (manifest.Files == null || !manifest.Files.Any()) {
                    AddFiles(basePath, @"**\*.*", null);
                }
                else {
                    foreach (var file in manifest.Files) {
                        AddFiles(basePath, file.Source, file.Target);
                    }
                }
            }
        }

        private void WriteManifest(Package package) {
            Uri uri = UriUtility.CreatePartUri(Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, TargetMode.Internal, Constants.SchemaNamespace + ManifestRelationType);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);

            using (Stream stream = packagePart.GetStream()) {
                Manifest manifest = Manifest.Create(this);
                manifest.Save(stream);
            }
        }

        private void WriteFiles(Package package) {
            // Add files that might not come from expanding files on disk
            foreach (IPackageFile file in Files) {
                using (Stream stream = file.GetStream()) {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private void AddFiles(string basePath, string source, string destination) {
            PathSearchFilter searchFilter = PathResolver.ResolveSearchFilter(basePath, source);
            IEnumerable<string> searchFiles = Directory.EnumerateFiles(searchFilter.SearchDirectory,
                                                          searchFilter.SearchPattern,
                                                          searchFilter.SearchOption);

            if (!searchFilter.WildCardSearch && !searchFiles.Any()) {
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture, NuGetResources.PackageAuthoring_FileNotFound,
                    source));
            }

            foreach (var file in searchFiles) {
                var destinationPath = PathResolver.ResolvePackagePath(searchFilter, file, destination);
                Files.Add(new PhysicalPackageFile {
                    SourcePath = file,
                    TargetPath = destinationPath
                });
            }
        }

        private static void CreatePart(Package package, string path, Stream sourceStream) {
            if (PackageUtility.IsManifest(path)) {
                return;
            }

            Uri uri = UriUtility.CreatePartUri(path);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);
            using (Stream stream = packagePart.GetStream()) {
                sourceStream.CopyTo(stream);
            }
        }

        /// <summary>
        /// Tags come in this format. tag1 tag2 tag3 etc..
        /// </summary>
        private static IEnumerable<string> ParseTags(string tags) {
            Debug.Assert(tags != null);
            return from tag in tags.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                   select tag.Trim();
        }

        public IPackage Build()
        {
            return new SimplePackage(this);
        }
    }
}