using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NuGet.Versioning;
using NuGetPe;
using NuGetPackageExplorer.Types;

namespace PackageExplorer
{
    [Export(typeof(IPluginManager))]
    internal class PluginManager : IPluginManager
    {
        private const string NuGetDirectoryName = "NuGet";
        private const string PluginsDirectoryName = "PackageExplorerPlugins";
        private const string DeleteMeExtension = ".deleteme";
        private static string[] FrameworkFolderForAssemblies = new string[] {
            "lib\\net40",
            "lib\\net45",
            "lib\\net451",
            "lib\\net452",
            "lib\\net46",
            "lib\\net461",
            "lib\\net462",
            "lib\\net47",
            "lib\\net471",
        };

        // %localappdata%/NuGet/PackageExplorerPlugins
        private static readonly string PluginsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            NuGetDirectoryName,
            PluginsDirectoryName
            );

        private AggregateCatalog _pluginCatalog;
        private Dictionary<PluginInfo, DirectoryCatalog> _pluginToCatalog;
        private List<PluginInfo> _plugins;

        public PluginManager(AggregateCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException("catalog");
            }

            // clean up from previous run
            DeleteAllDeleteMeFiles();
            EnsurePluginCatalog(catalog);
        }

        [Import]
        public Lazy<IUIServices> UIServices { get; set; }

        #region IPluginManager Members

        public ICollection<PluginInfo> Plugins
        {
            get { return _plugins; }
        }

        public PluginInfo AddPlugin(IPackage plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            try
            {
                var pluginInfo = new PluginInfo(plugin.Id, plugin.Version);

                var targetPath = GetTargetPath(pluginInfo);
                if (Directory.Exists(targetPath))
                {
                    UIServices.Value.Show(
                        "Adding plugin failed. There is already an existing plugin with the same Id and Version.",
                        MessageLevel.Error);
                }
                else
                {
                    Directory.CreateDirectory(targetPath);
                    if (Directory.Exists(targetPath))
                    {
                        // make sure there is no .delete file lurking around for this plugin
                        var deleteMePath = targetPath + DeleteMeExtension;
                        File.Delete(deleteMePath);

                        // copy assemblies
                        var numberOfFilesCopied = 
                            FrameworkFolderForAssemblies.Sum(folder => plugin.UnpackPackage(folder, targetPath));

                        if (numberOfFilesCopied == 0)
                        {
                            Directory.Delete(targetPath);
                            UIServices.Value.Show(
                                "Adding plugin failed. The selected package does not have any assembly inside the 'lib\\net*' folder.",
                                MessageLevel.Error);
                        }
                        else
                        {
                            var succeeded = AddPluginToCatalog(pluginInfo, targetPath, quietMode: false);
                            if (!succeeded)
                            {
                                DeletePlugin(pluginInfo);
                            }
                            return succeeded ? pluginInfo : null;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                UIServices.Value.Show(exception.Message, MessageLevel.Error);
            }

            return null;
        }

        public bool DeletePlugin(PluginInfo plugin)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            var targetPath = GetTargetPath(plugin);
            if (Directory.Exists(targetPath))
            {
                RemovePluginFromCatalog(plugin);

                try
                {
                    CreateDeleteMeFile(targetPath);
                    return true;
                }
                catch (IOException ex)
                {
                    UIServices.Value.Show(ex.Message, MessageLevel.Error);
                    return false;
                }
            }

            return false;
        }

        #endregion

        private void EnsurePluginCatalog(AggregateCatalog mainCatalog)
        {
            if (_pluginCatalog != null)
            {
                return;
            }

            var pluginDirectoryInfo = new DirectoryInfo(PluginsDirectory);
            if (!pluginDirectoryInfo.Exists)
            {
                // creates the plugins directory if it doesn't exist
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var nugetDirectory = CreateChildDirectory(new DirectoryInfo(localAppData), NuGetDirectoryName);
                CreateChildDirectory(nugetDirectory, PluginsDirectoryName);
            }

            _plugins = new List<PluginInfo>(GetAllPlugins());
            _pluginToCatalog = new Dictionary<PluginInfo, DirectoryCatalog>();
            _pluginCatalog = new AggregateCatalog();

            for (var i = _plugins.Count - 1; i >= 0; i--)
            {
                var pluginInfo = _plugins[i];
                var succeeded = AddPluginToCatalog(pluginInfo, GetTargetPath(pluginInfo), quietMode: true);
                if (!succeeded)
                {
                    _plugins.RemoveAt(i);
                    DeletePlugin(pluginInfo);
                }
            }

            mainCatalog.Catalogs.Add(_pluginCatalog);
        }

        private IEnumerable<PluginInfo> GetAllPlugins()
        {
            var directoryInfo = new DirectoryInfo(PluginsDirectory);
            if (directoryInfo.Exists)
            {
                return directoryInfo.GetDirectories().Select(ConvertFromDirectoryToPluginInfo).Where(p => p != null);
            }
            else
            {
                return Enumerable.Empty<PluginInfo>();
            }
        }

        private bool AddPluginToCatalog(PluginInfo pluginInfo, string targetPath, bool quietMode)
        {
            try
            {
                var directoryCatalog = new DirectoryCatalog(targetPath);
                if (directoryCatalog.Parts.Any())
                {
                    _pluginCatalog.Catalogs.Add(directoryCatalog);
                    _pluginToCatalog[pluginInfo] = directoryCatalog;
                }
                return true;
            }
            catch (ReflectionTypeLoadException exception)
            {
                _pluginToCatalog.Remove(pluginInfo);

                var errorMessage = BuildErrorMessage(exception);
                if (quietMode)
                {
                    Trace.WriteLine(errorMessage, "Plugins Loader");
                }
                else
                {
                    UIServices.Value.Show(errorMessage, MessageLevel.Error);
                }

                return false;
            }
        }

        private void RemovePluginFromCatalog(PluginInfo pluginInfo)
        {
            if (_pluginToCatalog.TryGetValue(pluginInfo, out var catalog))
            {
                _pluginCatalog.Catalogs.Remove(catalog);
            }
        }

        private static DirectoryInfo CreateChildDirectory(DirectoryInfo parentInfo, string path)
        {
            // if the child directory doesn't exist, create it
            var child = parentInfo.EnumerateDirectories(path, SearchOption.TopDirectoryOnly).FirstOrDefault() ??
                                  parentInfo.CreateSubdirectory(path);
            return child;
        }

        private PluginInfo ConvertFromDirectoryToPluginInfo(DirectoryInfo directory)
        {
            var name = directory.Name;
            const string regex = @"^(.+)\[(.+?)\]$";
            var match = Regex.Match(name, regex);
            if (match.Success)
            {
                var id = match.Groups[1].Value;
                var versionString = match.Groups[2].Value;
                if (NuGetVersion.TryParse(versionString, out var version))
                {
                    return new PluginInfo(id, version);
                }
            }

            return null;
        }

        private static string GetTargetPath(PluginInfo plugin)
        {
            var pluginName = plugin.Id + "[" + plugin.Version + "]";
            return Path.Combine(PluginsDirectory, pluginName);
        }

        private static void CreateDeleteMeFile(string targetPath)
        {
            if (targetPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                targetPath = targetPath.Substring(0, targetPath.Length - 1);
            }

            // when a plugin assembly is loaded by the app, we can't delete it directly.
            // instead, we create a .deleteme file at the same location and delete it when the app exits.
            var deleteMeFile = targetPath + DeleteMeExtension;
            File.WriteAllText(deleteMeFile, string.Empty);
        }

        private static void DeleteAllDeleteMeFiles()
        {
            try
            {
                var pluginDirectoryInfo = new DirectoryInfo(PluginsDirectory);
                if (pluginDirectoryInfo.Exists)
                {
                    var deleteMeFiles = pluginDirectoryInfo.EnumerateFiles("*" + DeleteMeExtension,
                                                                                             SearchOption.
                                                                                                 TopDirectoryOnly);
                    foreach (var file in deleteMeFiles)
                    {
                        // delete the .deleteme file
                        file.Delete();

                        // also delete the real plugin directory
                        var pluginDirectory = Path.Combine(PluginsDirectory,
                                                              Path.GetFileNameWithoutExtension(file.Name));
                        if (Directory.Exists(pluginDirectory))
                        {
                            Directory.Delete(pluginDirectory, recursive: true);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // should not throw any exception when deleting files.
                // ignore any of them.
            }
        }

        private static string BuildErrorMessage(ReflectionTypeLoadException exception)
        {
            var builder = new StringBuilder("One or more errors occurred while loading the selected plugin:");
            builder.AppendLine();
            builder.AppendLine();

            foreach (var loaderException in exception.LoaderExceptions)
            {
                builder.AppendLine(loaderException.Message);
            }

            return builder.ToString();
        }
    }
}
