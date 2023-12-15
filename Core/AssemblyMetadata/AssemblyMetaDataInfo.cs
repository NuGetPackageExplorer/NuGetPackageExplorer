using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGetPe.AssemblyMetadata
{
    /// <summary>
    /// Meta data of the assembly, 
    /// </summary>
    public class AssemblyMetaDataInfo
    {
        private readonly List<KeyValuePair<string, string>> _metadataEntries = new List<KeyValuePair<string, string>>();
        public IReadOnlyList<KeyValuePair<string, string>> MetadataEntries => _metadataEntries;
        public string FullName { get; internal set; }
        public string StrongName { get; internal set; }
        public IEnumerable<AssemblyName> ReferencedAssemblies { get; private set; } = Enumerable.Empty<AssemblyName>();

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public AssemblyDebugData DebugData { get; internal set; }
        public AssemblyMetaDataInfo(AssemblyName assemblyName)
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
        internal void SetReferencedAssemblyNames(IEnumerable<AssemblyName> referencedAssemblyNames)
        {
            ReferencedAssemblies = referencedAssemblyNames ?? throw new ArgumentNullException(nameof(referencedAssemblyNames));
        }

        /// <summary>
        /// Add arbitrary metadata information.
        /// </summary>
        internal void AddMetadata(string displayName, string value)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                throw new ArgumentNullException(nameof(displayName));
            }

            _metadataEntries.Add(new KeyValuePair<string, string>(displayName, value));
        }
    }
}
