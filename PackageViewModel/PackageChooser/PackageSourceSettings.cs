using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    internal class PackageSourceSettings : ISourceSettings {
        private const string NuGetFeed = "https://go.microsoft.com/fwlink/?LinkID=206669";

        private ISettingsManager _settingsManager;

        public PackageSourceSettings(ISettingsManager settingsManager) {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;
        }

        public IList<string> GetSources() {
            return _settingsManager.GetPackageSources();
        }

        public void SetSources(IEnumerable<string> sources) {
            _settingsManager.SetPackageSources(sources);
        }

        public string DefaultSource {
            get { return NuGetFeed; }
        }

        public string ActiveSource {
            get {
                return _settingsManager.ActivePackageSource;
            }
            set {
                _settingsManager.ActivePackageSource = value;
            }
        }
    }
}
