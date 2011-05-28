namespace NuGet {
    using System.IO;
    using System.Xml.Linq;

    internal static class XmlUtility {
        internal static XDocument GetOrCreateDocument(XName rootName, IFileSystem fileSystem, string path) {
            if (fileSystem.FileExists(path)) {
                try {
                    using (Stream configSream = fileSystem.OpenFile(path)) {
                        return XDocument.Load(configSream);
                    }
                }
                catch (FileNotFoundException) {
                    return CreateDocument(rootName, fileSystem, path);
                }
            }
            return CreateDocument(rootName, fileSystem, path);
        }

        private static XDocument CreateDocument(XName rootName, IFileSystem fileSystem, string path) {
            XDocument document = new XDocument(new XElement(rootName));
            // Add it to the file system
            fileSystem.AddFile(path, document.Save);         
            return document;
        }
    }
}
