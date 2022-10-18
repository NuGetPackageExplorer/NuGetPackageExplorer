using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NuGet.Versioning;

namespace NuGetPe
{
    public static class ManifestUtility
    {
        private const string TokenStart = "TOKENSTART";
        private const string TokenEnd = "TOKENEND";
        private const string TokenMetadataStart = "0.0.0-" + TokenStart + ".";
        private const string TokenMetadataEnd = "." + TokenEnd;
        private static readonly Regex TokenRegex = new Regex(@"([$])(?:(?=(\\?))\2.)*?\1", RegexOptions.Compiled);
        private static readonly Regex MetadataRegEx = new Regex($@"0\.0\.0\-{TokenStart}\.([^.]+)\.{TokenEnd}", RegexOptions.Compiled);

        public static Stream ReadManifest(string file)
        {
            using var str = File.OpenRead(file);
            return ReadManifest(str);
        }


        public static bool IsTokenized(this NuGetVersion version)
        {
            if (version is null)
                throw new System.ArgumentNullException(nameof(version));
            var labels = version.ReleaseLabels.ToList();

            return labels.Count >= 3 && labels[0] == TokenStart && labels[labels.Count - 1] == TokenEnd;
        }

        public static Stream ReadManifest(Stream stream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            // Also needs to handle blank versions
            var xdoc = XDocument.Load(stream);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Get the version node
            var version = xdoc.Root?.Descendants(ns + "version").FirstOrDefault();
            if (version is not null)
            {
                version.Value = ReplaceTokenWithMetadata(version.Value);
            }

            // Get dependency nodes
            var deps = xdoc.Root?.Descendants(ns + "dependency");
            if(deps is not null)
            {
                foreach (var dep in deps)
                {
                    var val = dep.GetOptionalAttributeValue("version");
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        dep.SetAttributeValue("version"!, ReplaceTokenWithMetadata(val));
                    }
                    else
                    {
                        // Some packages (like Paket.Core have version="" in the dependencies. NuGet doesn't handle it, so remove it so we can load.
                        dep.RemoveAttributes(a => string.Equals("version", a.Name.LocalName, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }            


            // The manifest utility exports licenseUrl for back compat, but it's not intended
            // for round-tripping
            // If license exists, strip licenseUrl


            if (xdoc.Root?.Descendants(ns + "license").Any() == true)
            {
                // Remove licenseUrl
                xdoc.Descendants(ns + "licenseUrl").Remove();
            }

            var ms = new MemoryStream();
            xdoc.Save(ms);
            ms.Position = 0;
            return ms;
        }

        /// <summary>
        /// Goes from $token$ to a marker value with 0.0.0+TOKENSTART.token.TOKENEND
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ReplaceTokenWithMetadata(string value)
        {
            if (value is null)
                throw new System.ArgumentNullException(nameof(value));

            // see if it's a token            

            var matches = TokenRegex.Matches(value);
#pragma warning disable CS8606 // Possible null reference assignment to iteration variable
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (Match match in matches)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8606 // Possible null reference assignment to iteration variable
            {
                var token = match!.Value[1..^1];
                value = value.Replace(match.Value, $"{TokenMetadataStart}{token}{TokenMetadataEnd}", System.StringComparison.Ordinal);
            }

            return value;
        }

        /// <summary>
        /// Goes from marker 0.0.0+TOKENSTART.token.TOKENEND to the token $token$
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string? ReplaceMetadataWithToken(string? value)
        {
            if (value == null)
            {
                return value;
            }

            // see if it's a token
            var matches = MetadataRegEx.Matches(value);

#pragma warning disable CS8606 // Possible null reference assignment to iteration variable
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            foreach (Match match in matches)
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8606 // Possible null reference assignment to iteration variable
            {
                var token = match!.Value[TokenMetadataStart.Length..^TokenMetadataEnd.Length];
                value = value.Replace(match.Value, $"${token}$", System.StringComparison.Ordinal);
            }

            return value;
        }

        public static void SaveToStream(Stream sourceStream, Stream destinationStream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            var xdoc = XDocument.Load(sourceStream);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            // Get the version node
            var version = xdoc.Root?.Descendants(ns + "version").FirstOrDefault();
            if (version is not null)
            {
                version.Value = ReplaceMetadataWithToken(version.Value)!;
            }

            // Get dependency nodes
            var deps = xdoc.Root?.Descendants(ns + "dependency");
            if(deps is not null)
            {
                foreach (var dep in deps)
                {
                    var val = dep.GetOptionalAttributeValue("version");
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        dep.SetAttributeValue("version"!, ReplaceMetadataWithToken(val));
                    }
                }
            }            

            // The manifest utility exports licenseUrl for back compat, but it's not intended
            // for round-tripping
            // If license exists, strip licenseUrl


            if (xdoc.Root?.Descendants(ns + "license").Any() == true)
            {
                // Remove licenseUrl
                xdoc.Descendants(ns + "licenseUrl").Remove();
            }

            xdoc.Save(destinationStream);
        }
    }
}
