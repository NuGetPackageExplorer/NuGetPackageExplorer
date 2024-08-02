using System;

using Microsoft.UI.Xaml.Media.Imaging;

namespace PackageExplorer
{
    public static class PackageImages
    {
        public static readonly BitmapImage DefaultPackageIcon;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static PackageImages()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            DefaultPackageIcon = new BitmapImage();

            DefaultPackageIcon.UriSource = new Uri("ms-appx:///Assets/default-package-icon.png");

            DefaultPackageIcon.DecodePixelWidth = 32;
            DefaultPackageIcon.DecodePixelHeight = 32;
        }
    }
}
