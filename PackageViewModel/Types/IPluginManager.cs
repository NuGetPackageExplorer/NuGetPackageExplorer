using System.Collections.Generic;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public interface IPluginManager {
        PluginInfo AddPlugin(IPackage plugin);
        bool DeletePlugin(PluginInfo plugin);
        ICollection<PluginInfo> Plugins { get; }
    }
}