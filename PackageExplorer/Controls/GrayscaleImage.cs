using System.Windows.Controls;

namespace PackageExplorer
{
    public class GrayscaleImage : Image
    {
        public GrayscaleImage()
        {
            Effect = new GrayscaleEffect.GrayscaleEffect { DesaturationFactor = 1 };
        }
    }
}