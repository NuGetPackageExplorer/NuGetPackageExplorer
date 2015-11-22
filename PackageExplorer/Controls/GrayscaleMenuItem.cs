using System.Windows;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class GrayscaleMenuItem : MenuItem
    {
        static GrayscaleMenuItem()
        {
            IconProperty.OverrideMetadata(
                typeof(GrayscaleMenuItem),
                new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnIconPropertyChanged)));
        }

        public GrayscaleMenuItem()
        {
            IsEnabledChanged += OnIsEnabledChanged;
        }

        private static void OnIconPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((GrayscaleMenuItem) sender).UpdateImageContent();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateImageContent();
        }

        private void UpdateImageContent()
        {
            var icon = Icon as Image;
            if (icon != null)
            {
                var effect = icon.Effect as GrayscaleEffect.GrayscaleEffect;
                if (effect != null)
                {
                    effect.DesaturationFactor = IsEnabled ? 1 : 0;
                }
            }
        }
    }
}