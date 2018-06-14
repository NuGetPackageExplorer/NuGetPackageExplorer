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

        public static Stream MakeSeekable(Stream stream)
        {
            if (stream.CanSeek)
            {
                return stream;
            }
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
    }
}
