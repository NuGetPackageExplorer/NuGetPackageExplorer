using System;
using System.Windows.Media.Imaging;

namespace PackageExplorer
{
    public static class Images
    {
        public static readonly BitmapImage DefaultPackageIcon;

#pragma warning disable CA1810 // Initialize reference type static fields inline
        static Images()
#pragma warning restore CA1810 // Initialize reference type static fields inline
        {
            DefaultPackageIcon = new BitmapImage();

            DefaultPackageIcon.BeginInit();
            DefaultPackageIcon.UriSource = new Uri("pack://application:,,,/NuGetPackageExplorer;component/Resources/default-package-icon.png");

            DefaultPackageIcon.DecodePixelWidth = 32;
            DefaultPackageIcon.DecodePixelHeight = 32;

            DefaultPackageIcon.EndInit();
            DefaultPackageIcon.Freeze();
        }
    }
}
