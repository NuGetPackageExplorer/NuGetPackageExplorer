using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NuGet.Resources;

namespace NuGet {
    internal static class ManifestSchemaUtility {
        public const string SchemaVersionV1 = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
        public const string SchemaVersionV2 = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";
        private const string SchemaNamespaceToken = "!!Schema version!!";

        private static readonly Dictionary<int, string> VersionToSchemaMappings = new Dictionary<int, string> {
            { 1, SchemaVersionV1 },
            { 2, SchemaVersionV2 }
        };

        // Mapping from schema to resource name
        private static readonly Dictionary<string, string> SchemaToResourceMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { SchemaVersionV1, "NuGet.Authoring.nuspec.xsd" },
            { SchemaVersionV2, "NuGet.Authoring.nuspec.xsd" }
        };


        public static string GetSchemaNamespace(int version) {
            string schemaNamespace;
            if (!VersionToSchemaMappings.TryGetValue(version, out schemaNamespace)) {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_InvalidSchemaNamespace,
                    version, typeof(ManifestSchemaUtility).AssemblyQualifiedName));
            }
            return schemaNamespace;
        }

        public static Stream GetSchemaStream(string schemaNamespace) {
            string schemaResourceName;
            if (!SchemaToResourceMappings.TryGetValue(schemaNamespace, out schemaResourceName)) {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, NuGetResources.Manifest_InvalidSchemaNamespace, 
                    schemaNamespace));
            }
            // Update the xsd with the right schema namespace
            var assembly = typeof(Manifest).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(schemaResourceName))) {
                string content = reader.ReadToEnd();
                return String.Format(CultureInfo.InvariantCulture, content, schemaNamespace).AsStream(); 
            }
        }

        public static bool IsKnownSchema(string schemaNamespace) {
            return SchemaToResourceMappings.ContainsKey(schemaNamespace);
        }
    }
}
