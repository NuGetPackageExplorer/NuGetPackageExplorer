using NuGet;

namespace PackageExplorerViewModel
{
    internal static class GalleryServerFactory
    {
        public static IGalleryServer CreateGalleryServer(string source, string userAgent, bool useV1Protocol)
        {
            if (useV1Protocol)
            {
                return new GalleryServer(source, userAgent);
            }
            else
            {
                return new GalleryServer2(source, userAgent);
            }
        }
    }
}