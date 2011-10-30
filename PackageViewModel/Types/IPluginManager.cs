using System.Collections.Generic;
using NuGet;

namespace NuGetPackageExplorer.Types {
    public interface IPluginManager {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "1#")]
        PluginInfo AddPlugin(IPackage plugin);
        bool DeletePlugin(PluginInfo plugin);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        IEnumerable<PluginInfo> GetAllPlugins();
    }
}