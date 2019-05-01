using System;
using System.Xml.Linq;

namespace NuGetPe
{
    internal static class XmlUtility
    {
        internal static XDocument GetOrCreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            if (fileSystem.FileExists(path))
            {
                try
                {
                    using var configSream = fileSystem.OpenFile(path);
                    return XDocument.Load(configSream);
                }
                catch (Exception)
                {
                    return CreateDocument(rootName, fileSystem, path);
                }
            }
            return CreateDocument(rootName, fileSystem, path);
        }

        private static XDocument CreateDocument(XName rootName, IFileSystem fileSystem, string path)
        {
            var document = new XDocument(new XElement(rootName));
            // Add it to the file system
            fileSystem.AddFile(path, document.Save);
            return document;
        }
    }
}
