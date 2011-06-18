using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    internal class PublishSourceSettings : ISourceSettings {
        private const string NuGetPublishFeed = "http://go.microsoft.com/fwlink/?LinkID=207106";

        private ISettingsManager _settingsManager;

        public PublishSourceSettings(ISettingsManager settingsManager) {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;
        }

        public IList<string> GetSources() {
            return _settingsManager.GetPublishSources();
        }

        public void SetSources(IEnumerable<string> sources) {
            _settingsManager.SetPublishSources(sources);
        }

        public string DefaultSource {
            get { return NuGetPublishFeed; }
        }

        public string ActiveSource {
            get {
                return _settingsManager.ActivePublishSource;
            }
            set {
                _settingsManager.ActivePublishSource = value;
            }
        }
    }
}
