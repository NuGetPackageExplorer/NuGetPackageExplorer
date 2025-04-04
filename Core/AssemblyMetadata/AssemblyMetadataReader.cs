﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    public static class AssemblyMetadataReader
    {
        /// <summary>
        /// Expects a PE file
        /// </summary>
        /// <param name="assemblyPath"></param>
        /// <returns></returns>
        public static AssemblyMetaDataInfo? ReadMetaData(string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                return null;
            }

            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            if (assemblyName == null)
            {
                return null;
            }

            var result = new AssemblyMetaDataInfo(assemblyName);

            // For WinRT component, we can only read Full Name. 
            if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
            {
                return result;
            }

            try
            {
                using var metadataParser = new AssemblyMetadataParser(assemblyPath);
                AddAssemblyAttributes(metadataParser, result);
                AddReferencedAssemblyInfo(metadataParser, result);
                result.DebugData = metadataParser.GetDebugData();
            }
            catch
            {
                // Ignore if unable to read metadata.
            }

            return result;
        }

        public static async Task<AssemblyDebugData> ReadDebugData(Stream? peStream, Stream pdbStream)
        {
            ArgumentNullException.ThrowIfNull(pdbStream);

            try
            {
                using var cts = new CancellationTokenSource();

                return await Task.Run(() =>
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(10));
                    using var reader = new AssemblyDebugParser(peStream, pdbStream);
                    return reader.GetDebugData();

                }, cts.Token).ConfigureAwait(false);
            }
            finally
            {
                if (peStream != null)
                {
                    await peStream.DisposeAsync().ConfigureAwait(false);
                }
                await pdbStream.DisposeAsync().ConfigureAwait(false);
            }

        }

        private static void AddAssemblyAttributes(AssemblyMetadataParser parser, AssemblyMetaDataInfo result)
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

        private static string? TryReadAttributeValue(AssemblyMetadataParser.AttributeInfo attribute)
        {
            // Skip InternalsVisibleToAttribute
            if (attribute.FullTypeName.Equals(typeof(InternalsVisibleToAttribute).FullName, StringComparison.Ordinal))
            {
                return null;
            }

            // Handle the metadata attrib
            if (attribute.FullTypeName.Equals(typeof(AssemblyMetadataAttribute).FullName, StringComparison.Ordinal))
            {
                return $"Key = {attribute.FixedArguments[0].Value}, Value = {attribute.FixedArguments[1].Value}";
            }

            if (attribute.FixedArguments.Length != 1 || attribute.NamedArguments.Length > 0)
            {
                return null;
            }

            var singleCtorParameter = attribute.FixedArguments[0];
            if (singleCtorParameter.Type.Equals(typeof(string).FullName, StringComparison.Ordinal))
            {
                var strValue = singleCtorParameter.Value!.ToString();
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

        private static void AddReferencedAssemblyInfo(AssemblyMetadataParser parser, AssemblyMetaDataInfo result)
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
