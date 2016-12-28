using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using NuGet;
using NuGetPe.Resources;

namespace NuGetPe
{
    [XmlType("package")]
    public class Manifest
    {
        private const string SchemaVersionAttributeName = "schemaVersion";

        public Manifest()
        {
            Metadata = new ManifestMetadata();
        }

        [XmlElement("metadata", IsNullable = false)]
        public ManifestMetadata Metadata { get; set; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement("files", IsNullable = true)]
        public ManifestFileList FilesList { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists",
            Justification = "It's easier to create a list")]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly",
            Justification = "This is needed for xml serialization")]
        [XmlIgnore]
        public List<ManifestFile> Files
        {
            get { return FilesList != null ? FilesList.Items : null; }
            set
            {
                if (FilesList == null)
                {
                    FilesList = new ManifestFileList();
                }
                FilesList.Items = value;
            }
        }

        public void Save(Stream stream)
        {
            Save(stream, validate: true, minimumManifestVersion: 1);
        }

        /// <summary>
        /// Saves the current manifest to the specified stream.
        /// </summary>
        /// <param name="stream">The target stream.</param>
        /// <param name="minimumManifestVersion">The minimum manifest version that this class must use when saving.</param>
        public void Save(Stream stream, int minimumManifestVersion)
        {
            Save(stream, validate: true, minimumManifestVersion: minimumManifestVersion);
        }

        public void Save(Stream stream, bool validate, int minimumManifestVersion)
        {
            if (validate)
            {
                // Validate before saving
                Validate(this);
            }

            int version = Math.Max(minimumManifestVersion, ManifestVersionUtility.GetManifestVersion(Metadata));
            string schemaNamespace = ManifestSchemaUtility.GetSchemaNamespace(version);

            // Define the namespaces to use when serializing
            var ns = new XmlSerializerNamespaces();
            ns.Add("", schemaNamespace);

            // Need to force the namespace here again as the default in order to get the XML output clean
            var serializer = new XmlSerializer(typeof(Manifest), schemaNamespace);
            using (var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
            {
                xmlWriter.Indentation = 4;
                xmlWriter.Formatting = Formatting.Indented;
                serializer.Serialize(xmlWriter, this, ns);
            }
        }

        // http://msdn.microsoft.com/en-us/library/53b8022e(VS.71).aspx
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeFilesList()
        {
            // This is to prevent the XML serializer from serializing 'null' value of FilesList as 
            // <files xsi:nil="true" />
            return FilesList != null;
        }

        public static Manifest ReadFrom(Stream stream)
        {
            // Read the document
            XDocument document = XDocument.Load(stream);
            string schemeNamespace = GetSchemaNamespace(document);

            foreach (XElement e in document.Descendants())
            {
                // Assign the schema namespace derived to all nodes in the document.
                e.Name = XName.Get(e.Name.LocalName, schemeNamespace);
            }

            // Validate the schema
            ValidateManifestSchema(document, schemeNamespace);

            // Serialize it
            var manifest = ManifestReader.ReadManifest(document);

            // Validate before returning
            Validate(manifest);

            return manifest;
        }

        private static string GetSchemaNamespace(XDocument document)
        {
            string schemaNamespace = ManifestSchemaUtility.SchemaVersionV1;
            XNamespace rootNameSpace = document.Root.Name.Namespace;
            if (rootNameSpace != null && !String.IsNullOrEmpty(rootNameSpace.NamespaceName))
            {
                schemaNamespace = rootNameSpace.NamespaceName;
            }
            return schemaNamespace;
        }

        public static Manifest Create(IPackageMetadata metadata)
        {
            return new Manifest
                   {
                       Metadata = new ManifestMetadata
                                  {
                                      Id = metadata.Id.SafeTrim(),
                                      Version = metadata.Version.ToStringSafe(),
                                      Title = metadata.Title.SafeTrim(),
                                      Authors = GetCommaSeparatedString(metadata.Authors),
                                      Owners = GetCommaSeparatedString(metadata.Owners) ??
                                               GetCommaSeparatedString(metadata.Authors),
                                      Tags = String.IsNullOrEmpty(metadata.Tags) ? null : metadata.Tags.SafeTrim(),
                                      LicenseUrl = 
                                          metadata.LicenseUrl != null
                                              ? metadata.LicenseUrl.OriginalString.
                                                    SafeTrim()
                                              : null,
                                      ProjectUrl =
                                          metadata.ProjectUrl != null
                                              ? metadata.ProjectUrl.OriginalString.
                                                    SafeTrim()
                                              : null,
                                      IconUrl =
                                          metadata.IconUrl != null
                                              ? metadata.IconUrl.OriginalString.
                                                    SafeTrim()
                                              : null,
                                      RequireLicenseAcceptance = metadata.RequireLicenseAcceptance,
                                      Serviceable = metadata.Serviceable,
                                      DevelopmentDependency = metadata.DevelopmentDependency,
                                      Description = metadata.Description.SafeTrim(),
                                      Copyright = metadata.Copyright.SafeTrim(),
                                      Summary = metadata.Summary.SafeTrim(),
                                      ReleaseNotes = metadata.ReleaseNotes.SafeTrim(),
                                      Language = metadata.Language.SafeTrim(),
                                      DependencySets = CreateDependencySet(metadata),
                                      FrameworkAssemblies = CreateFrameworkAssemblies(metadata),
                                      ReferenceSets = CreateReferenceSets(metadata),
                                      MinClientVersionString = metadata.MinClientVersion.ToStringSafe()
                                  }
                   };
        }

        private static List<ManifestDependencySet> CreateDependencySet(IPackageMetadata metadata)
        {
            if (metadata.DependencySets == null)
            {
                return null;
            }

            return (from dependencySet in metadata.DependencySets
                    select new ManifestDependencySet
                    {
                        TargetFramework = dependencySet.TargetFramework != null ? VersionUtility.GetFrameworkString(dependencySet.TargetFramework) : null,
                        Dependencies = CreateDependencies(dependencySet.Dependencies)
                    }).ToList();
        }

        private static List<ManifestDependency> CreateDependencies(ICollection<PackageDependency> dependencies)
        {
            if (dependencies == null)
            {
                return new List<ManifestDependency>(0);
            }

            return (from dependency in dependencies
                    select new ManifestDependency
                    {
                        Id = dependency.Id.SafeTrim(),
                        Version = dependency.VersionSpec.ToStringSafe(),
                        Exclude = dependency.Exclude.SafeTrim()
                    }).ToList();
        }

        private static List<ManifestFrameworkAssembly> CreateFrameworkAssemblies(IPackageMetadata metadata)
        {
            return metadata.FrameworkAssemblies == null ||
                   !metadata.FrameworkAssemblies.Any()
                       ? null
                       : (from reference in metadata.FrameworkAssemblies
                          select new ManifestFrameworkAssembly
                                 {
                                     AssemblyName = reference.AssemblyName,
                                     TargetFramework =
                                         String.Join(", ",
                                                     reference.SupportedFrameworks.
                                                         Select(
                                                             VersionUtility.
                                                                 GetFrameworkString))
                                 }).ToList();
        }

        private static List<ManifestReferenceSet> CreateReferenceSets(IPackageMetadata metadata)
        {
            return (from referenceSet in metadata.PackageAssemblyReferences
                    select new ManifestReferenceSet
                    {
                        TargetFramework = referenceSet.TargetFramework != null ? VersionUtility.GetFrameworkString(referenceSet.TargetFramework) : null,
                        References = CreateReferences(referenceSet)
                    }).ToList();
        }

        private static List<ManifestReference> CreateReferences(PackageReferenceSet referenceSet)
        {
            if (referenceSet.References == null)
            {
                return new List<ManifestReference>();
            }

            return (from reference in referenceSet.References
                    select new ManifestReference { File = reference.SafeTrim() }).ToList();
        }

        private static string GetCommaSeparatedString(IEnumerable<string> values)
        {
            if (values == null || !values.Any())
            {
                return null;
            }
            return String.Join(",", values);
        }

        private static void ValidateManifestSchema(XDocument document, string schemaNamespace)
        {
            CheckSchemaVersion(document);

            // Create the schema set
            var schemaSet = ManifestSchemaUtility.GetManifestSchemaSet(schemaNamespace);

            // Validate the document
            document.Validate(schemaSet, (sender, e) =>
                                         {
                                             if (e.Severity == XmlSeverityType.Error)
                                             {
                                                 // Throw an exception if there is a validation error
                                                 throw new InvalidOperationException(e.Message);
                                             }
                                         });
        }

        private static void CheckSchemaVersion(XDocument document)
        {
            // Get the metadata node and look for the schemaVersion attribute
            XElement metadata = GetMetadataElement(document);

            if (metadata != null)
            {
                // Yank this attribute since we don't want to have to put it in our xsd
                XAttribute schemaVersionAttribute = metadata.Attribute(SchemaVersionAttributeName);

                if (schemaVersionAttribute != null)
                {
                    schemaVersionAttribute.Remove();
                }

                // Get the package id from the metadata node
                string packageId = GetPackageId(metadata);

                // If the schema of the document doesn't match any of our known schemas
                if (!ManifestSchemaUtility.IsKnownSchema(document.Root.Name.Namespace.NamespaceName))
                {
                    throw new InvalidOperationException(
                        String.Format(CultureInfo.CurrentCulture,
                                      NuGetResources.IncompatibleSchema,
                                      packageId,
                                      typeof(Manifest).Assembly.GetNameSafe().Version));
                }
            }
        }

        private static string GetPackageId(XElement metadataElement)
        {
            XName idName = XName.Get("id", metadataElement.Document.Root.Name.NamespaceName);
            XElement element = metadataElement.Element(idName);

            if (element != null)
            {
                return element.Value;
            }

            return null;
        }

        private static XElement GetMetadataElement(XDocument document)
        {
            // Get the metadata element this way so that we don't have to worry about the schema version
            XName metadataName = XName.Get("metadata", document.Root.Name.Namespace.NamespaceName);

            return document.Root.Element(metadataName);
        }

        internal static void Validate(Manifest manifest)
        {
            var results = new List<ValidationResult>();

            // Run all data annotations validations
            TryValidate(manifest.Metadata, results);
            TryValidate(manifest.Files, results);
            if (manifest.Metadata.DependencySets != null)
            {
                TryValidate(manifest.Metadata.DependencySets.SelectMany(d => d.Dependencies), results);
            }
            TryValidate(manifest.Metadata.ReferenceSets, results);

            if (results.Any())
            {
                string message = String.Join(Environment.NewLine, results.Select(r => r.ErrorMessage));
                throw new ValidationException(message);
            }

            // Validate additional dependency rules dependencies
            ValidateDependencySets(manifest.Metadata);
        }

        private static void ValidateDependencySets(IPackageMetadata metadata)
        {
            foreach (var dependencySet in metadata.DependencySets)
            {
                var dependencyHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var dependency in dependencySet.Dependencies)
                {
                    // Throw an error if this dependency has been defined more than once
                    if (!dependencyHash.Add(dependency.Id))
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DuplicateDependenciesDefined, metadata.Id, dependency.Id));
                    }

                    // Validate the dependency version
                    ValidateDependencyVersion(dependency);
                }
            }
        }

        private static void ValidateDependencyVersion(PackageDependency dependency)
        {
            if (dependency.VersionSpec != null)
            {
                if (dependency.VersionSpec.MinVersion != null &&
                    dependency.VersionSpec.MaxVersion != null)
                {

                    if ((!dependency.VersionSpec.IsMaxInclusive ||
                         !dependency.VersionSpec.IsMinInclusive) &&
                        dependency.VersionSpec.MaxVersion == dependency.VersionSpec.MinVersion)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }

                    if (dependency.VersionSpec.MaxVersion < dependency.VersionSpec.MinVersion)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.DependencyHasInvalidVersion, dependency.Id));
                    }
                }
            }
        }

        private static bool TryValidate(object value, ICollection<ValidationResult> results)
        {
            if (value != null)
            {
                var enumerable = value as IEnumerable;
                if (enumerable != null)
                {
                    foreach (object item in enumerable)
                    {
                        Validator.TryValidateObject(item, CreateValidationContext(item), results);
                    }
                }
                return Validator.TryValidateObject(value, CreateValidationContext(value), results);
            }
            return true;
        }

        private static ValidationContext CreateValidationContext(object value)
        {
            return new ValidationContext(value, NullServiceProvider.Instance, new Dictionary<object, object>());
        }

        #region Nested type: NullServiceProvider

        private class NullServiceProvider : IServiceProvider
        {
            private static readonly IServiceProvider _instance = new NullServiceProvider();

            public static IServiceProvider Instance
            {
                get { return _instance; }
            }

            #region IServiceProvider Members

            public object GetService(Type serviceType)
            {
                return null;
            }

            #endregion
        }

        #endregion
    }
}