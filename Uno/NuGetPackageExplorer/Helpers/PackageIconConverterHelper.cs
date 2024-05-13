using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PackageExplorerViewModel;
using Microsoft.UI.Xaml.Media.Imaging;
using PackageExplorer;
using Uno.Extensions;
using Uno.Logging;
using NuGetPe;

namespace PackageExplorer
{
    public class PackageIconConverterHelper
    {
        private static readonly IconUrlConverterHelper IconUrlConverter = new IconUrlConverterHelper();

        public static BitmapSource? ConvertToImage(PackageViewModel package, string iconOrIconUrl)
        {
            // note: unused `iconOrIconUrl` is used as a refresh trigger
            if (package?.IsDisposed == false && !string.IsNullOrEmpty(iconOrIconUrl))
            {
                var metadata = package.PackageMetadata;

                if (!string.IsNullOrEmpty(metadata.Icon))
                {
                    // Normalize any directories to match what's the package
                    // We do this here instead of the metadata so that we round-trip
                    // whatever the user originally had when in edit view
                    var iconPath = metadata.Icon.Replace('/', '\\');
                    foreach (var file in package.RootFolder.GetFiles())
                    {
                        if (string.Equals(file.Path, iconPath, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using var stream = StreamUtility.MakeSeekable(file.GetStream(), true);

                                var image = new BitmapImage();
                                image.SetSource(stream.AsRandomAccessStream());

                                return image;
                            }
                            catch (Exception e)
                            {
                                typeof(PackageIconConverterHelper).Log().Error($"failed to open icon file: {metadata.Icon}", e);
                            }
                        }
                    }
                }

                if (metadata.IconUrl != null)
                {
                    return IconUrlConverter.ConvertToBitmapSource(metadata.IconUrl?.ToString()!, PackageImages.DefaultPackageIcon);
                }
            }

            return PackageImages.DefaultPackageIcon;
        }

        public static BitmapSource? ConvertToImage(string iconUrl)
        {
            return IconUrlConverter.ConvertToBitmapSource(iconUrl, PackageImages.DefaultPackageIcon);
        }
    }
}
