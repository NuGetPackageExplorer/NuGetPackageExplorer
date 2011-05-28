using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    public class PluginManagerViewModel : INotifyPropertyChanged, IComparer<FileInfo> {

        private SortedCollection<FileInfo> _plugins;
        private readonly IPluginManager _pluginManager;
        private readonly IUIServices _uiServices;

        public PluginManagerViewModel(IPluginManager pluginManager, IUIServices uiServices) {
            if (pluginManager == null) {
                throw new ArgumentNullException("pluginManager");
            }
            _pluginManager = pluginManager;
            _uiServices = uiServices;

            DeleteCommand = new RelayCommand<FileInfo>(DeleteCommandExecute, DeleteCommandCanExecute);
            AddCommand = new RelayCommand(AddCommandExecute);
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public ICollection<FileInfo> Plugins {
            get {
                if (_plugins == null) {
                    _plugins = new SortedCollection<FileInfo>(_pluginManager.GetAllPlugins(), this);
                }
                return _plugins;
            }
        }

        public RelayCommand<FileInfo> DeleteCommand { get; private set; }

        public RelayCommand AddCommand { get; private set; }

        private void AddCommandExecute() {
            string selectedFile;
            bool result = _uiServices.OpenFileDialog(
                "Select Plugin Assembly",
                ".NET assemblies (*.dll)|*.dll",
                out selectedFile);

            if (result) {
                FileInfo file;
                bool succeeded = _pluginManager.AddPluginFromAssembly(selectedFile, out file);
                if (succeeded) {
                    Plugins.Add(file);
                }
            }
        }

        private void DeleteCommandExecute(FileInfo file) {
            bool succeeded = _pluginManager.DeletePlugin(file);
            if (succeeded) {
                Plugins.Remove(file);
            }
        }

        private bool DeleteCommandCanExecute(FileInfo file) {
            return file != null;
        }

        public int Compare(FileInfo x, FileInfo y) {
            return String.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
