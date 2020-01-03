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
            ((GrayscaleMenuItem)sender).UpdateImageContent();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateImageContent();
        }

        private void UpdateImageContent()
        {
            if (Icon is Image icon)
            {
                if (icon.Effect is GrayscaleEffect.GrayscaleEffect effect)
                {
                    effect.DesaturationFactor = IsEnabled ? 1 : 0;
                }
            }

            if (Icon is ContentPresenter cp)
            {
                if (cp.Effect is GrayscaleEffect.GrayscaleEffect effect)
                {
                    effect.DesaturationFactor = IsEnabled ? 1 : 0;
                }
            }
        }
    }
}
