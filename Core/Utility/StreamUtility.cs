using System.IO;
using System.Text;

namespace NuGetPe
{
    public static class StreamUtility
    {
        public static Stream ToStream(string content)
        {
            System.ArgumentNullException.ThrowIfNull(content);

            return new MemoryStream(Encoding.UTF8.GetBytes(content));
        }

        public static Stream MakeSeekable(Stream stream, bool disposeOriginal = false)
        {
            System.ArgumentNullException.ThrowIfNull(stream);

            if (stream.CanSeek)
            {
                return stream;
            }

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            if (disposeOriginal)
            {
                stream.Dispose();
            }
            return memoryStream;
        }
    }
}
