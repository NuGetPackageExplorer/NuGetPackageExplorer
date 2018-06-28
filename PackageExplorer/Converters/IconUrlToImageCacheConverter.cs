using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PackageExplorer
{
    public class IconUrlToImageCacheConverter : IValueConverter
    {
        private const int DecodePixelWidth = 32;

        private static readonly ObjectCache BitmapImageCache = MemoryCache.Default;

        private static readonly WebExceptionStatus[] FatalErrors = {
                                                                       WebExceptionStatus.ConnectFailure,
                                                                       WebExceptionStatus.RequestCanceled,
                                                                       WebExceptionStatus.ConnectionClosed,
                                                                       WebExceptionStatus.Timeout,
                                                                       WebExceptionStatus.UnknownError
                                                                   };

        private static readonly System.Net.Cache.RequestCachePolicy RequestCacheIfAvailable = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.CacheIfAvailable);

        private static readonly ErrorFloodGate ErrorFloodGate = new ErrorFloodGate();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var iconUrl = value as string;
            var defaultPackageIcon = parameter as BitmapSource;
            if (string.IsNullOrWhiteSpace(iconUrl))
            {
                return null;
            }

            if (BitmapImageCache.Get(iconUrl) is BitmapSource cachedBitmapImage)
            {
                return cachedBitmapImage;
            }

            // Some people run on networks with internal NuGet feeds, but no access to the package images on the internet.
            // This is meant to detect that kind of case, and stop spamming the network, so the app remains responsive.
            if (ErrorFloodGate.IsOpen)
            {
                return defaultPackageIcon;
            }

            var iconBitmapImage = new BitmapImage();
            iconBitmapImage.BeginInit();
            iconBitmapImage.UriSource = new Uri(iconUrl);

            // Default cache policy: Per MSDN, satisfies a request for a resource either by using the cached copy of the resource or by sending a request
            // for the resource to the server. The action taken is determined by the current cache policy and the age of the content in the cache.
            // This is the cache level that should be used by most applications.
            iconBitmapImage.UriCachePolicy = RequestCacheIfAvailable;

            // Instead of scaling larger images and keeping larger image in memory, this makes it so we scale it down, and throw away the bigger image.
            // Only need to set this on one dimension, to preserve aspect ratio
            iconBitmapImage.DecodePixelWidth = DecodePixelWidth;

            iconBitmapImage.DecodeFailed += IconBitmapImage_DownloadOrDecodeFailed;
            iconBitmapImage.DownloadFailed += IconBitmapImage_DownloadOrDecodeFailed;
            iconBitmapImage.DownloadCompleted += IconBitmapImage_DownloadCompleted;

            try
            {
                iconBitmapImage.EndInit();
            }
            // if the URL is a file: URI (which actually happened!), we'll get an exception.
            // if the URL is a file: URI which is in an existing directory, but the file doesn't exist, we'll fail silently.
            catch (Exception)
            {
                iconBitmapImage = null;
            }
            finally
            {
                // store this bitmapImage in the bitmap image cache, so that other occurances can reuse the BitmapImage
                cachedBitmapImage = iconBitmapImage ?? defaultPackageIcon;
                AddToCache(iconUrl, cachedBitmapImage);

                ErrorFloodGate.ReportAttempt();
            }

            return cachedBitmapImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        private static void AddToCache(string iconUrl, BitmapSource iconBitmapImage)
        {
            var policy = new CacheItemPolicy
            {
                SlidingExpiration = TimeSpan.FromMinutes(10),
                RemovedCallback = CacheEntryRemoved
            };
            BitmapImageCache.Set(iconUrl, iconBitmapImage, policy);
        }

        private static void CacheEntryRemoved(CacheEntryRemovedArguments arguments)
        {

        }

        private void IconBitmapImage_DownloadCompleted(object sender, EventArgs e)
        {
            if (sender is BitmapImage bitmapImage && !bitmapImage.IsFrozen)
            {
                bitmapImage.Freeze();
            }
        }

        private void IconBitmapImage_DownloadOrDecodeFailed(object sender, System.Windows.Media.ExceptionEventArgs e)
        {
            // Fix the bitmap image cache to have default package icon, if some other failure didn't already do that.
            if (!(sender is BitmapImage bitmapImage))
            {
                return;
            }

            var cachedBitmapImage = BitmapImageCache.Get(bitmapImage.UriSource.ToString()) as BitmapSource;
            if (cachedBitmapImage != Images.DefaultPackageIcon)
            {
                AddToCache(bitmapImage.UriSource.ToString(), Images.DefaultPackageIcon);

                if (e.ErrorException is WebException webex && FatalErrors.Any(status => webex.Status == status))
                {
                    ErrorFloodGate.ReportError();
                }
            }
        }
    }
}
