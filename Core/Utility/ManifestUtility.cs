using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NuGetPe.Utility
{
    public static class ManifestUtility
    {
        public const string TokenMetadata = "0.0.0+TOKEN999.";

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
                var val = version.Value;

                // see if it's a token
                if (val.Length >= 3 && val[0] == '$' && val[val.Length - 1] == '$')
                {
                    var token = val.Substring(1, val.Length - 2);
                    version.Value = $"{TokenMetadata}{token}";
                }
            }

            var ms = new MemoryStream();
            xdoc.Save(ms);
            ms.Position = 0;
            return ms;
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
                var val = version.Value;

                // see if it's a token
                if (val.StartsWith(TokenMetadata))
                {
                    var token = val.Substring(TokenMetadata.Length);
                    version.Value = $"${token}$";
                }
            }

            xdoc.Save(destinationStream);
        }

        // public static 
    }
}
