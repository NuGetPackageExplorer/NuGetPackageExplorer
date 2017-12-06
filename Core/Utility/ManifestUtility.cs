using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetPe.Utility
{
    public static class ManifestUtility
    {
        public const string TokenMetadataStart = "0.0.0+TOKENSTART.";
        public const string TokenMetadataEnd = ".TOKENEND";
        static readonly Regex tokenRegex = new Regex(@"([$])(?:(?=(\\?))\2.)*?\1", RegexOptions.Compiled);
        static readonly Regex metadataRegEx = new Regex(@"0\.0\.0\+TOKENSTART\.([^.]+)\.TOKENEND", RegexOptions.Compiled);

        public static Stream ReadManifest(string file)
        {
            return ReadManifest(File.OpenRead(file));
        }

        public static Stream ReadManifest(Stream stream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            var xdoc = XDocument.Load(stream);
            var ns = xdoc.Root.GetDefaultNamespace();

            // Get the version node
            var version = xdoc.Root.Descendants(ns + "version").FirstOrDefault();
            if (version != null)
            {
                version.Value = ReplaceTokenWithMetadata(version.Value);
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
            // see if it's a token

            var matches = tokenRegex.Matches(value);
            foreach (Match match in matches)
            {
                var token = match.Value.Substring(1, match.Value.Length - 2);
                value = value.Replace(match.Value, $"{TokenMetadataStart}{token}{TokenMetadataEnd}");
            }

            return value;
        }

        /// <summary>
        /// Goes from marker 0.0.0+TOKENSTART.token.TOKENEND to the token $token$
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ReplaceMetadataWithToken(string value)
        {

            // see if it's a token
            var matches = metadataRegEx.Matches(value);

            foreach (Match match in matches)
            {
                var token = match.Value.Substring(TokenMetadataStart.Length, match.Value.Length - TokenMetadataEnd.Length - TokenMetadataStart.Length);
                value = value.Replace(match.Value, $"${token}$");
            }

            return value;
        }

        public static void SaveToStream(Stream sourceStream, Stream destinationStream)
        {
            // This method needs to replace tokens in version fields with a sentinel value
            // since the NuGetVersion object model doesn't support it.
            var xdoc = XDocument.Load(sourceStream);
            var ns = xdoc.Root.GetDefaultNamespace();

            // Get the version node
            var version = xdoc.Root.Descendants(ns + "version").FirstOrDefault();
            if (version != null)
            {
                version.Value = ReplaceMetadataWithToken(version.Value);
            }

            xdoc.Save(destinationStream);
        }
    }
}
