using System.IO;
using System.Text;

namespace PackageExplorer
{
    internal static class StreamUtility
    {
        public static Stream ToStream(string content)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }
    }
}