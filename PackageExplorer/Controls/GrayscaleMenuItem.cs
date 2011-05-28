using System.Windows.Controls;

namespace PackageExplorer {
    public class GrayscaleMenuItem : MenuItem {
        public GrayscaleMenuItem() {
            IsEnabledChanged += OnIsEnabledChanged;
        }

        private void OnIsEnabledChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e) {
            var icon = Icon as Image;
            if (icon != null) {
                var effect = icon.Effect as GrayscaleEffect.GrayscaleEffect;
                if (effect != null) {
                    bool isEnabled = (bool)e.NewValue;
                    effect.DesaturationFactor = isEnabled ? 1 : 0;
                }
            }
        }
    }
}