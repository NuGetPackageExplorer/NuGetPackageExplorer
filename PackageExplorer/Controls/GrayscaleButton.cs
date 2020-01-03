using System.Windows;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class GrayscaleButton : Button
    {
        public GrayscaleButton()
        {
            IsEnabledChanged += OnIsEnabledChanged;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);
            UpdateImageContent();
        }

        private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateImageContent();
        }

        private void UpdateImageContent()
        {
            if (Content is Image icon)
            {
                if (icon.Effect is GrayscaleEffect.GrayscaleEffect effect)
                {
                    effect.DesaturationFactor = IsEnabled ? 1 : 0;
                }
            }
        }
    }
}