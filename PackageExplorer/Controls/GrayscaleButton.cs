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
            var icon = Content as Image;
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