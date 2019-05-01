using System.IO;
using System.Text;

namespace NuGetPe
{
    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            var length = (int)stream.Length;
            var buffer = new byte[length];
            stream.Read(buffer, 0, length);
            return buffer;
        }

        public static string ReadToEnd(this Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }

        public static Stream AsStream(this string value)
        {
            return AsStream(value, Encoding.Default);
        }

        public static Stream AsStream(this string value, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(value));
        }
    }
}