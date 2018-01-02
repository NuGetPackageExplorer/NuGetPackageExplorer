namespace NuGetPe
{
    internal static class StringExtensions
    {
        public static string SafeTrim(this string value)
        {
            return value?.Trim();
        }
    }
}
