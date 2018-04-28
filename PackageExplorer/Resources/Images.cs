using System;
using System.Windows.Media.Imaging;

namespace PackageExplorer
{
    public static class Images
    {
        public static readonly BitmapImage DefaultPackageIcon;

        static Images()
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
