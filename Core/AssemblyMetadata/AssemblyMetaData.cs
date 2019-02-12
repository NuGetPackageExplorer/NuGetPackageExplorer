using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGetPe.AssemblyMetadata
{
    /// <summary>
    /// Meta data of the assembly, 
    /// </summary>
    public class AssemblyMetaData
    {
        private Dictionary<string, string> MetadataEntries { get; } = new Dictionary<string, string>();
        private string FullName { get; set; }
        private string StrongName { get; set; }
        private IEnumerable<AssemblyName> ReferencedAsseblies { get; set; } = Enumerable.Empty<AssemblyName>();
        public AssemblyDebugData? DebugData { get; internal set; }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public AssemblyMetaData(AssemblyName assemblyName)
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
        {
            if (assemblyName == null)
            {
                throw new ArgumentNullException(nameof(assemblyName));
            }

            SetFullName(assemblyName);
        }

        /// <summary>
        /// Set Fullname of the assembly and determine strong name.
        /// </summary>
        /// <remarks>Helper</remarks>
        private void SetFullName(AssemblyName assemblyName)
        {
            FullName = assemblyName.FullName;

            try
            {
                var publicKey = assemblyName.GetPublicKeyToken();
                var isStrongNamed = publicKey != null && publicKey.Length > 0;

                StrongName = isStrongNamed
                    ? $"Yes, version {assemblyName.Version}"
                    : "No";
            }
            catch
            {
                StrongName = "No"; // Default if we can't read it
            }
        }

        /// <summary>
        /// Set list of referenced assembly names.
        /// </summary>
        public void SetReferencedAssemblyNames(IEnumerable<AssemblyName> referencedAssemblyNames)
        {
            ReferencedAsseblies = referencedAssemblyNames ?? throw new ArgumentNullException(nameof(referencedAssemblyNames));
        }

        /// <summary>
        /// Add arbitrary metadata information.
        /// </summary>
        public void AddMetadata(string displayName, string value)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            MetadataEntries[displayName] = value;
        }

        /// <summary>
        /// Gets all the metadata entries sorted by importance
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> GetMetadataEntriesOrderedByImportance()
        {
            if (FullName != null)
            {
                yield return MakePair("Full Name", FullName);
            }
            if (StrongName != null)
            {
                yield return MakePair("Strong Name", StrongName);
            }

            foreach (var entry in MetadataEntries.OrderBy(kv => kv.Key))
            {
                yield return entry;
            }

            if (ReferencedAsseblies != null)
            {
                var assemblyNamesDelimitedByLineBreak = string.Join(
                    Environment.NewLine,
                    ReferencedAsseblies
                        .OrderBy(assName => assName.Name)
                        .Select(assName => assName.FullName));

                yield return MakePair("Referenced assemblies", assemblyNamesDelimitedByLineBreak);
            }
        }

        private static KeyValuePair<string, string> MakePair(string displayName, string value)
        {
            return new KeyValuePair<string, string>(displayName, value);
        }
    }
}
