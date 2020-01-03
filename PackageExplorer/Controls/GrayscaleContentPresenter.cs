using System.Windows;
using System.Windows.Controls;

namespace PackageExplorer
{
    public class GrayscaleContentPresenter : ContentPresenter
    {
        public GrayscaleContentPresenter()
        {
            Effect = new GrayscaleEffect.GrayscaleEffect { DesaturationFactor = 1 };
        }
    }
}
