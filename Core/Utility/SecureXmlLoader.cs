using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NuGetPe
{
    internal static class SecureXmlLoader
    {
        private const long MaxDocumentCharacters = 10 * 1024 * 1024;

        private static readonly XmlReaderSettings ReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersFromEntities = 0,
            MaxCharactersInDocument = MaxDocumentCharacters
        };

        public static XDocument Load(Stream stream)
        {
            using var reader = XmlReader.Create(stream, ReaderSettings);
            return XDocument.Load(reader);
        }
    }
}
