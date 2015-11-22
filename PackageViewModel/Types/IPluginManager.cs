using System.Collections.Generic;
using NuGet;

namespace NuGetPackageExplorer.Types
{
    public interface IPluginManager
    {
        ICollection<PluginInfo> Plugins { get; }
        PluginInfo AddPlugin(IPackage plugin);
        bool DeletePlugin(PluginInfo plugin);
    }
}