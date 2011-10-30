using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(IPluginManager))]
    internal class PluginManager : IPluginManager {
        private const string NuGetDirectoryName = "NuGet";
        private const string PluginsDirectoryName = "PackageExplorerPlugins";
        private const string DeleteMeExtension = ".deleteme";
        private const string FrameworkFolderForAssemblies = "lib\\net40";

        private Dictionary<PluginInfo, DirectoryCatalog> _pluginToCatalog;

        // %localappdata%/NuGet/PackageExplorerPlugins
        private static readonly string PluginsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            NuGetDirectoryName,
            PluginsDirectoryName
        );

        private AggregateCatalog _pluginCatalog;

        [Import]
        public Lazy<IUIServices> UIServices { get; set; }

        public PluginManager(AggregateCatalog catalog) {
            if (catalog == null) {
                throw new ArgumentNullException("catalog");
            }

            // clean up from previous run
            DeleteAllDeleteMeFiles();
            EnsurePluginCatalog(catalog);
        }

        public PluginInfo AddPlugin(IPackage plugin) {
            if (plugin == null)
            {
                throw new ArgumentNullException("plugin");
            }

            try
            {
                var pluginInfo = new PluginInfo(plugin.Id, plugin.Version);

                string targetPath = GetTargetPath(pluginInfo);
                if (Directory.Exists(targetPath))
                {
                    UIServices.Value.Show("Adding plugin failed. There is already an existing plugin with the same Id and Version.", MessageLevel.Error);
                }
                else
                {
                    Directory.CreateDirectory(targetPath);
                    if (Directory.Exists(targetPath))
                    {
                        // make sure there is no .delete file lurking around for this plugin
                        string deleteMePath = targetPath + DeleteMeExtension;
                        File.Delete(deleteMePath);

                        // copy assemblies
                        int numberOfFilesCopied = plugin.UnpackPackage(FrameworkFolderForAssemblies, targetPath);
                        if (numberOfFilesCopied == 0)
                        {
                            Directory.Delete(targetPath);
                            UIServices.Value.Show("Adding plugin failed. The selected package does not have any assembly inside the 'lib\\net40' folder.", MessageLevel.Error);
                        }
                        else
                        {
                            bool succeeded = AddPluginToCatalog(pluginInfo, targetPath);
                            if (!succeeded)
                            {                               
                                Directory.Delete(targetPath, recursive: true);
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

        public bool DeletePlugin(PluginInfo plugin) {
            if (plugin == null) {
                throw new ArgumentNullException("plugin");
            }

            string targetPath = GetTargetPath(plugin);
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

        public IEnumerable<PluginInfo> GetAllPlugins() {
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

        private bool AddPluginToCatalog(PluginInfo pluginInfo, string targetPath) {
            try {
                var directoryCatalog = new DirectoryCatalog(targetPath);
                if (directoryCatalog.Parts.Any()) {
                    _pluginCatalog.Catalogs.Add(directoryCatalog);
                    _pluginToCatalog[pluginInfo] = directoryCatalog;
                }
                return true;
            }
            catch (ReflectionTypeLoadException exception) {
                _pluginToCatalog.Remove(pluginInfo);

                Debug.WriteLine("{0}", new[] { exception.Message });
                foreach (var loaderException in exception.LoaderExceptions) {
                    Debug.WriteLine("\t{0}", new[] { loaderException.Message });
                }
                return false;
            }
        }

        private void RemovePluginFromCatalog(PluginInfo pluginInfo) {
            DirectoryCatalog catalog;
            if (_pluginToCatalog.TryGetValue(pluginInfo, out catalog)) {
                _pluginCatalog.Catalogs.Remove(catalog);
            }
        }

        private void EnsurePluginCatalog(AggregateCatalog mainCatalog) {
            if (_pluginCatalog != null) {
                return;
            }

            DirectoryInfo pluginDirectoryInfo = new DirectoryInfo(PluginsDirectory);
            if (!pluginDirectoryInfo.Exists) {
                // creates the plugins directory if it doesn't exist
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                DirectoryInfo nugetDirectory = CreateChildDirectory(new DirectoryInfo(localAppData), NuGetDirectoryName);
                pluginDirectoryInfo = CreateChildDirectory(nugetDirectory, PluginsDirectoryName);
            }

            _pluginToCatalog = new Dictionary<PluginInfo, DirectoryCatalog>();
            _pluginCatalog = new AggregateCatalog();
            foreach (PluginInfo pluginInfo in GetAllPlugins())
            {
                AddPluginToCatalog(pluginInfo, GetTargetPath(pluginInfo));
            }
            mainCatalog.Catalogs.Add(_pluginCatalog);
        }

        private DirectoryInfo CreateChildDirectory(DirectoryInfo parentInfo, string path) {
            DirectoryInfo child = parentInfo.EnumerateDirectories(path, SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (child == null) {
                // if the child directory doesn't exist, create it
                child = parentInfo.CreateSubdirectory(path);
            }
            return child;
        }

        private PluginInfo ConvertFromDirectoryToPluginInfo(DirectoryInfo directory)
        {
            string name = directory.Name;
            string regex = @"^(.+)\[(.+?)\]$";
            Match match = Regex.Match(name, regex);
            if (match.Success)
            {
                string id = match.Groups[1].Value;
                string versionString = match.Groups[2].Value;
                SemanticVersion version;
                if (SemanticVersion.TryParse(versionString, out version))
                {
                    return new PluginInfo(id, version);
                }
            }

            return null;
        }

        private static string GetTargetPath(PluginInfo plugin)
        {
            string pluginName = plugin.Id + "[" + plugin.Version.ToString() + "]";
            return Path.Combine(PluginsDirectory, pluginName);
        }

        private void CreateDeleteMeFile(string targetPath) {
            if (targetPath.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
            {
                targetPath = targetPath.Substring(0, targetPath.Length - 1);
            }

            // when a plugin assembly is loaded by the app, we can't delete it directly.
            // instead, we create a .deleteme file at the same location and delete it when the app exits.
            string deleteMeFile = targetPath + DeleteMeExtension;
            File.WriteAllText(deleteMeFile, String.Empty);
        }

        private void DeleteAllDeleteMeFiles() {
            try {
                var pluginDirectoryInfo = new DirectoryInfo(PluginsDirectory);
                if (pluginDirectoryInfo.Exists) {
                    var deleteMeFiles = pluginDirectoryInfo.EnumerateFiles("*" + DeleteMeExtension, SearchOption.TopDirectoryOnly);
                    foreach (var file in deleteMeFiles) {
                        // delete the .deleteme file
                        file.Delete();

                        // also delete the real plugin directory
                        string pluginDirectory = Path.Combine(PluginsDirectory, Path.GetFileNameWithoutExtension(file.Name));
                        if (Directory.Exists(pluginDirectory)) {
                            Directory.Delete(pluginDirectory, recursive: true);
                        }
                    }
                }
            }
            catch (Exception) {
                // should not throw any exception when deleting files.
                // ignore any of them.
            }
        }
    }
}