using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NuGet.Resources;

namespace NuGet
{
    internal static class ManifestSchemaUtility
    {
        private const string SchemaNamespaceToken = "!!Schema version!!";

        /// <summary>
        /// Baseline schema 
        /// </summary>
        internal const string SchemaVersionV1 = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";

        /// <summary>
        /// Added copyrights, references and release notes
        /// </summary>
        internal const string SchemaVersionV2 = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";

        /// <summary>
        /// Used if the version is a semantic version.
        /// </summary>
        internal const string SchemaVersionV3 = "http://schemas.microsoft.com/packaging/2011/10/nuspec.xsd";

        private static readonly string[] VersionToSchemaMappings = new[]
                                                                   {
                                                                       SchemaVersionV1,
                                                                       SchemaVersionV2,
                                                                       SchemaVersionV3,
                                                                   };

        // Mapping from schema to resource name
        private static readonly Dictionary<string, string> SchemaToResourceMappings =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    SchemaVersionV1,
                    "NuGet.Authoring.nuspec.xsd"
                    },
                {
                    SchemaVersionV2,
                    "NuGet.Authoring.nuspec.xsd"
                    },
                {
                    SchemaVersionV3,
                    "NuGet.Authoring.nuspec.xsd"
                    },
            };

        public static string GetSchemaNamespace(int version)
        {
            // Versions are internally 0-indexed but stored with a 1 index so decrement it by 1
            if (version <= 0 || version > VersionToSchemaMappings.Length)
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                                                                  "Unknown schema version '{0}'.", version));
            }
            return VersionToSchemaMappings[version - 1];
        }

        public static Stream GetSchemaStream(string schemaNamespace)
        {
            string schemaResourceName;
            if (!SchemaToResourceMappings.TryGetValue(schemaNamespace, out schemaResourceName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                                          NuGetResources.Manifest_InvalidSchemaNamespace,
                                                          schemaNamespace));
            }
            // Update the xsd with the right schema namespace
            Assembly assembly = typeof(Manifest).Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(schemaResourceName)))
            {
                string content = reader.ReadToEnd();
                return String.Format(CultureInfo.InvariantCulture, content, schemaNamespace).AsStream();
            }
        }

        public static bool IsKnownSchema(string schemaNamespace)
        {
            return SchemaToResourceMappings.ContainsKey(schemaNamespace);
        }
    }
}