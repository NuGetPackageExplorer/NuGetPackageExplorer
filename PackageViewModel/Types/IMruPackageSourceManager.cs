using System.Collections.ObjectModel;

namespace NuGetPackageExplorer.Types
{
    public interface IMruPackageSourceManager
    {
        string ActivePackageSource { get; set; }
        ObservableCollection<string> PackageSources { get; }
        void NotifyPackageSourceAdded(string newSource);
        void OnApplicationExit();
    }
}
