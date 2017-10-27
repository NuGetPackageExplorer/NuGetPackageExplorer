using System;
using System.Collections.Generic;
using System.Reflection;

namespace NuGetPe.AssemblyMetadata
{
    /// <summary>
    /// Meta data of the assembly, 
    /// </summary>
    public class AssemblyMetaData : Dictionary<string, string>
    {
        public const string ReferencedAssembliesKey = "Referenced assemblies";

        /// <summary>
        /// Set Fullname of the assembly and determine strong name.
        /// </summary>
        /// <remarks>Helper</remarks>
        public void SetFullName(AssemblyName assemblyName)
        {
            this[FullNameLabel] = assemblyName.FullName;

            try
            {
                var publicKey = assemblyName.GetPublicKeyToken();
                var isStrongNamed = publicKey != null && publicKey.Length > 0;

                if (isStrongNamed)
                {
                    this[StrongNamedLabel] = $"Yes, version {assemblyName.Version}";
                }
                else
                {
                    this[StrongNamedLabel] = "No";
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private const string FullNameLabel = "Full Name";
        private const string StrongNamedLabel = "Strong Name";
    }
}
