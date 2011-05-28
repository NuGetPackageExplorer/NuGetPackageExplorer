using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using NuGetPackageExplorer.Types;

namespace PackageExplorer {

    [Export(typeof(IPluginManager))]
    internal class PluginManager : IPluginManager, IEqualityComparer<FileInfo> {
        private const string NuGetDirectoryName = "NuGet";
        private const string PluginsDirectoryName = "PackageExplorerPlugins";
        private const string DeleteMeExtension = ".deleteme";

        private Dictionary<FileInfo, AssemblyCatalog> _pluginToCatalog;

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

        public bool AddPluginFromAssembly(string assemblyPath, out FileInfo file) {
            file = null;
            if (File.Exists(assemblyPath)) {
                try {
                    string assemblyName = Path.GetFileName(assemblyPath);
                    string targetPath = Path.Combine(PluginsDirectory, assemblyName);
                    if (File.Exists(targetPath)) {
                        UIServices.Value.Show("Adding plugin assembly failed. There is already an existing assembly with the same name.", MessageLevel.Error);
                    }
                    else {
                        File.Copy(assemblyPath, targetPath);
                        if (File.Exists(targetPath)) {
                            // make sure there is no .delete file lurking around for this plugin
                            string deleteMePath = targetPath + DeleteMeExtension;
                            File.Delete(deleteMePath);

                            file = new FileInfo(targetPath);
                            AddPluginToCatalog(file);
                            return true;
                        }
                    }
                }
                catch (Exception exception) {
                    UIServices.Value.Show(exception.Message, MessageLevel.Error);
                }
            }

            return false;
        }

        public bool DeletePlugin(FileInfo file) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            if (!file.Exists || file.IsReadOnly) {
                return false;
            }

            bool confirmed = UIServices.Value.Confirm(
                "Confirm deleting " + file.Name, 
                "Do you really want to delete this plugin?");

            if (!confirmed) {
                return false;
            }
            
            try {
                RemovePluginFromCatalog(file);
                CreateDeleteMeFile(file);
                return true;
            }
            catch (IOException ex) {
                UIServices.Value.Show(ex.Message, MessageLevel.Error);
                return false;
            }
        }

        public IEnumerable<FileInfo> GetAllPlugins() {
            var directoryInfo = new DirectoryInfo(PluginsDirectory);
            if (directoryInfo.Exists) {
                return directoryInfo.EnumerateFiles("*.dll", SearchOption.TopDirectoryOnly);
            }
            else {
                return Enumerable.Empty<FileInfo>();
            }
        }

        private void AddPluginToCatalog(FileInfo pluginFile) {
            var fileCatalog = new AssemblyCatalog(pluginFile.FullName);
            _pluginCatalog.Catalogs.Add(fileCatalog);
            _pluginToCatalog[pluginFile] = fileCatalog;
        }

        private void RemovePluginFromCatalog(FileInfo pluginFile) {
            AssemblyCatalog catalog;
            if (_pluginToCatalog.TryGetValue(pluginFile, out catalog)) {
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

            _pluginToCatalog = new Dictionary<FileInfo, AssemblyCatalog>(this);
            _pluginCatalog = new AggregateCatalog();
            foreach (FileInfo pluginFile in GetAllPlugins()) {
                AddPluginToCatalog(pluginFile);
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

        private void CreateDeleteMeFile(FileInfo file) {
            // when a plugin assembly is loaded by the app, we can't delete it directly.
            // instead, we create a .deleteme file at the same location and delete it when the app exits.
            string deleteMeFile = file.FullName + DeleteMeExtension;
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

                        // also delete the real plugin file
                        string pluginFile = Path.Combine(PluginsDirectory, Path.GetFileNameWithoutExtension(file.Name));
                        if (File.Exists(pluginFile)) {
                            File.Delete(pluginFile);
                        }
                    }
                }
            }
            catch (Exception) {
                // should not throw any exception when deleting files.
                // ignore any of them.
            }
        }

        public bool Equals(FileInfo x, FileInfo y) {
            return x.FullName.Equals(y.FullName, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(FileInfo obj) {
            return obj.FullName.GetHashCode();
        }
    }
}