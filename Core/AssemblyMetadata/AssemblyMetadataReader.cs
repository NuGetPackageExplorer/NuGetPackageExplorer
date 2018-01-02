using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace NuGetPe.AssemblyMetadata
{
    public static class AssemblyMetadataReader
    {
        public static AssemblyMetaData ReadMetaData(string assemblyPath)
        {
            if (assemblyPath == null) return null;
            
            var result = new AssemblyMetaData();
            
            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            if (assemblyName == null)
            {
                return result;
            }
            
            result.SetFullName(assemblyName);

            // For WinRT component, we can only read Full Name. 
            if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
            {
                return result;
            }
            
            try
            {
                using (var metadataParser = new AssemblyMetadataParser(assemblyPath))
                {
                    AddAssemblyAttributes(metadataParser, result);
                    AddReferencedAssemblyInfo(metadataParser, result);
                }
            }
            catch
            {
                // Ingore if unable to read metadata.
            }

            return result;
        }

        private static void AddAssemblyAttributes(AssemblyMetadataParser parser, AssemblyMetaData result)
        {
            try
            {  
                foreach (var attribute in parser.GetAssemblyAttributes())
                {
                    var value = TryReadAttributeValue(attribute);
                    if (value == null)
                    {
                        continue;
                    }

                    var displayName = ReadAttributeDisplayName(attribute);
                    result.AddMetadata(displayName, value);
                }
            }
            catch (Exception)
            {
                // Silently abort the process if unable to fetch all the attributes.
            }
        }
        
        private static string TryReadAttributeValue(AssemblyMetadataParser.AttributeInfo attribute)
        {
            // Skip InternalsVisibleToAttribute
            if (attribute.FullTypeName.Equals(typeof(InternalsVisibleToAttribute).FullName, StringComparison.Ordinal))
            {
                return null;
            }

            if (attribute.FixedArguments.Length != 1 || attribute.NamedArguments.Length > 0)
            {
                return null;
            }

            var singleCtorParameter = attribute.FixedArguments[0];
            if (singleCtorParameter.Type.Equals(typeof(string).FullName, StringComparison.Ordinal))
            {
                var strValue = singleCtorParameter.Value.ToString();
                return string.IsNullOrEmpty(strValue)
                    ? null
                    : strValue;
            }

            return null;
        }

        private static string ReadAttributeDisplayName(AssemblyMetadataParser.AttributeInfo attribute)
        {
            var shortName = attribute.FullTypeName.Split(".+".ToCharArray()).Last();

            const string attributeSuffix = "Attribute";
            
            return shortName.EndsWith(attributeSuffix, StringComparison.Ordinal)
                ? shortName.Substring(0, shortName.Length - attributeSuffix.Length)
                : shortName;
        }

        private static void AddReferencedAssemblyInfo(AssemblyMetadataParser parser, AssemblyMetaData result)
        {
            try
            {
                // ToArray is required, otherwise sequence might be enumerated when context is already closed.
                var referencedAssemblyNames = parser.GetReferencedAssemblyNames().ToArray();
                result.SetReferencedAssemblyNames(referencedAssemblyNames);
            }
            catch
            {
                // Ignore if unable to fetch information.
            }
        }
    }
}