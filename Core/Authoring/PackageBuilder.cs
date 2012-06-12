using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using NuGet.Resources;

namespace NuGet
{
    public sealed class PackageBuilder : IPackageBuilder
    {
        private const string DefaultContentType = "application/octet";
        internal const string ManifestRelationType = "manifest";

        public PackageBuilder(string path)
            : this(path, Path.GetDirectoryName(path))
        {
        }

        public PackageBuilder(string path, string basePath)
            : this()
        {
            using (Stream stream = File.OpenRead(path))
            {
                ReadManifest(stream, basePath);
            }
        }

        public PackageBuilder(Stream stream, string basePath)
            : this()
        {
            ReadManifest(stream, basePath);
        }

        public PackageBuilder()
        {
            Files = new Collection<IPackageFile>();
            Dependencies = new Collection<PackageDependency>();
            FrameworkReferences = new Collection<FrameworkAssemblyReference>();
            PackageAssemblyReferences = new Collection<AssemblyReference>();
            Authors = new HashSet<string>();
            Owners = new HashSet<string>();
            Tags = new HashSet<string>();
        }

        public ISet<string> Authors { get; private set; }

        public ISet<string> Owners { get; private set; }
        public ISet<string> Tags { get; private set; }

        public Collection<PackageDependency> Dependencies { get; private set; }
        public Collection<AssemblyReference> PackageAssemblyReferences { get; private set; }

        public Collection<FrameworkAssemblyReference> FrameworkReferences { get; private set; }

        #region IPackageBuilder Members

        public string Id { get; set; }

        public SemanticVersion Version { get; set; }

        public string Title { get; set; }

        public Uri IconUrl { get; set; }

        public Uri LicenseUrl { get; set; }

        public Uri ProjectUrl { get; set; }

        public bool RequireLicenseAcceptance { get; set; }

        public string Description { get; set; }

        public string Summary { get; set; }

        public string ReleaseNotes { get; set; }

        public string Copyright { get; set; }

        public string Language { get; set; }

        public Collection<IPackageFile> Files { get; private set; }

        IEnumerable<string> IPackageMetadata.Authors
        {
            get { return Authors; }
        }

        IEnumerable<string> IPackageMetadata.Owners
        {
            get { return Owners; }
        }

        string IPackageMetadata.Tags
        {
            get { return String.Join(" ", Tags); }
        }

        IEnumerable<AssemblyReference> IPackageMetadata.References
        {
            get { return PackageAssemblyReferences; }
        }

        IEnumerable<PackageDependency> IPackageMetadata.Dependencies
        {
            get { return Dependencies; }
        }

        IEnumerable<FrameworkAssemblyReference> IPackageMetadata.FrameworkAssemblies
        {
            get { return FrameworkReferences; }
        }

        public void Save(Stream stream)
        {
            // Make sure we're saving a valid package id
            PackageIdValidator.ValidatePackageId(Id);

            // Throw if the package doesn't contain any dependencies nor content
            if (!Files.Any() && !Dependencies.Any() && !FrameworkReferences.Any())
            {
                throw new InvalidOperationException(NuGetResources.CannotCreateEmptyPackage);
            }

            ValidateDependencies(Version, Dependencies);
            ValidateReferenceAssemblies(Files, PackageAssemblyReferences);

            using (Package package = Package.Open(stream, FileMode.Create))
            {
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
                package.PackageProperties.Keywords = ((IPackageMetadata) this).Tags;
                package.PackageProperties.Title = Title;
                package.PackageProperties.Subject = "NuGet Package Explorer";
            }
        }

        #endregion

        internal static void ValidateDependencies(SemanticVersion version, IEnumerable<PackageDependency> dependencies)
        {
            if (version == null)
            {
                // We have independent validation for null-versions.
                return;
            }

            if (String.IsNullOrEmpty(version.SpecialVersion))
            {
                // If we are creating a production package, do not allow any of the dependencies to be a prerelease version.
                PackageDependency prereleaseDependency = dependencies.FirstOrDefault(IsPrereleaseDependency);
                if (prereleaseDependency != null)
                {
                    throw new InvalidDataException(String.Format(CultureInfo.CurrentCulture,
                                                                 NuGetResources.Manifest_InvalidPrereleaseDependency,
                                                                 prereleaseDependency));
                }
            }
        }

        internal static void ValidateReferenceAssemblies(IEnumerable<IPackageFile> files,
                                                         IEnumerable<AssemblyReference> packageAssemblyReferences)
        {
            var libFiles = new HashSet<string>(from file in files
                                               where
                                                   !String.IsNullOrEmpty(file.Path) &&
                                                   file.Path.StartsWith("lib\\", StringComparison.OrdinalIgnoreCase)
                                               select Path.GetFileName(file.Path), StringComparer.OrdinalIgnoreCase);

            foreach (AssemblyReference reference in packageAssemblyReferences)
            {
                if (!libFiles.Contains(reference.File) && !libFiles.Contains(reference + ".dll") &&
                    !libFiles.Contains(reference + ".exe"))
                {
                    throw new InvalidDataException(
                        String.Format(
                            CultureInfo.CurrentCulture,
                            NuGetResources.Manifest_InvalidReference,
                            reference.File));
                }
            }
        }

        private static bool IsPrereleaseDependency(PackageDependency dependency)
        {
            IVersionSpec versionSpec = dependency.VersionSpec;
            if (versionSpec != null)
            {
                return (versionSpec.MinVersion != null &&
                        !String.IsNullOrEmpty(dependency.VersionSpec.MinVersion.SpecialVersion)) ||
                       (versionSpec.MaxVersion != null &&
                        !String.IsNullOrEmpty(dependency.VersionSpec.MaxVersion.SpecialVersion));
            }
            return false;
        }

        private void ReadManifest(Stream stream, string basePath)
        {
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
            ReleaseNotes = metadata.ReleaseNotes;
            Language = metadata.Language;
            Copyright = metadata.Copyright;

            if (metadata.Tags != null)
            {
                Tags.AddRange(ParseTags(metadata.Tags));
            }

            Dependencies.AddRange(metadata.Dependencies);
            FrameworkReferences.AddRange(metadata.FrameworkAssemblies);
            if (metadata.References != null)
            {
                PackageAssemblyReferences.AddRange(metadata.References.Select(r => new AssemblyReference(r.File)));
            }

            // If there's no base path then ignore the files node
            if (basePath != null)
            {
                if (manifest.Files == null)
                {
                    AddFiles(basePath, @"**\*.*", null);
                }
                else
                {
                    foreach (ManifestFile file in manifest.Files)
                    {
                        AddFiles(basePath, file.Source, file.Target, file.Exclude);
                    }
                }
            }
        }

        private void WriteManifest(Package package)
        {
            Uri uri = UriUtility.CreatePartUri(Id + Constants.ManifestExtension);

            // Create the manifest relationship
            package.CreateRelationship(uri, TargetMode.Internal,
                                       Constants.PackageRelationshipNamespace + ManifestRelationType);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);

            using (Stream stream = packagePart.GetStream())
            {
                Manifest manifest = Manifest.Create(this);
                if (PackageAssemblyReferences.Any())
                {
                    manifest.Metadata.References = new List<ManifestReference>(
                        PackageAssemblyReferences.Select(reference => new ManifestReference {File = reference.File}));
                }
                manifest.Save(stream);
            }
        }

        private void WriteFiles(Package package)
        {
            // Add files that might not come from expanding files on disk
            foreach (IPackageFile file in new HashSet<IPackageFile>(Files))
            {
                using (Stream stream = file.GetStream())
                {
                    CreatePart(package, file.Path, stream);
                }
            }
        }

        private void AddFiles(string basePath, string source, string destination, string exclude = null)
        {
            List<PhysicalPackageFile> searchFiles =
                PathResolver.ResolveSearchPattern(basePath, source, destination).ToList();
            ExcludeFiles(searchFiles, basePath, exclude);

            if (!PathResolver.IsWildcardSearch(source) && !searchFiles.Any())
            {
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture,
                                                              NuGetResources.PackageAuthoring_FileNotFound,
                                                              source));
            }
            Files.AddRange(searchFiles);
        }

        private static void ExcludeFiles(List<PhysicalPackageFile> searchFiles, string basePath, string exclude)
        {
            if (String.IsNullOrEmpty(exclude))
            {
                return;
            }

            // One or more exclusions may be specified in the file. Split it and prepend the base path to the wildcard provided.
            string[] exclusions = exclude.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in exclusions)
            {
                string wildCard = PathResolver.NormalizeWildcard(basePath, item);
                PathResolver.FilterPackageFiles(searchFiles, p => p.SourcePath, new[] {wildCard});
            }
        }

        private static void CreatePart(Package package, string path, Stream sourceStream)
        {
            if (PackageUtility.IsManifest(path))
            {
                return;
            }

            Uri uri = UriUtility.CreatePartUri(path);

            // Create the part
            PackagePart packagePart = package.CreatePart(uri, DefaultContentType, CompressionOption.Maximum);
            using (Stream stream = packagePart.GetStream())
            {
                sourceStream.CopyTo(stream);
            }
        }

        /// <summary>
        /// Tags come in this format. tag1 tag2 tag3 etc..
        /// </summary>
        private static IEnumerable<string> ParseTags(string tags)
        {
            Debug.Assert(tags != null);
            return from tag in tags.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)
                   select tag.Trim();
        }

        public IPackage Build()
        {
            return new SimplePackage(this);
        }
    }
}