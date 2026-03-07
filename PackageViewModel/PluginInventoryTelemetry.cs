using System;
using System.Collections.Generic;
using System.Linq;

using NuGetPackageExplorer.Types;

using NuGetPe;

namespace PackageExplorerViewModel
{
    public static class PluginInventoryTelemetry
    {
        public static bool TryTrack(Func<IPluginManager> resolvePluginManager, out Exception? error)
        {
            ArgumentNullException.ThrowIfNull(resolvePluginManager);

            try
            {
                Track(resolvePluginManager());
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex;
                return false;
            }
        }

        private static void Track(IPluginManager pluginManager)
        {
            ArgumentNullException.ThrowIfNull(pluginManager);

            var plugins = pluginManager.Plugins
                .Select(static plugin => plugin.Id + "@" + plugin.Version)
                .OrderBy(static plugin => plugin, StringComparer.OrdinalIgnoreCase)
                .ToList();

            DiagnosticsClient.TrackEvent(
                "PluginInventory",
                new Dictionary<string, string> { { "plugins", string.Join(";", plugins) } },
                new Dictionary<string, double> { { "pluginCount", plugins.Count } });
        }
    }
}
