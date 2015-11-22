using System.Configuration;

namespace PackageExplorer.Properties
{
    public sealed partial class Settings
    {
        protected override void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e)
        {
            // coerse the value of FontSize
            if (FontSize < 12 || FontSize > 18)
            {
                FontSize = 12;
            }
        }
    }
}