namespace NuGetPe
{
    internal static class ObjectExtensions
    {
        public static string ToStringSafe(this object obj)
        {
            return obj?.ToString();
        }
    }
}