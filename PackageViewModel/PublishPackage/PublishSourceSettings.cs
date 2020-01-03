using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal class PublishSourceSettings : ISourceSettings
    {
        private readonly ISettingsManager _settingsManager;

        public PublishSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;
        }

        #region ISourceSettings Members

        public IList<string> GetSources()
        {
            var sources = _settingsManager.GetPublishSources();
            for (var i = 0; i < sources.Count; i++)
            {
                sources[i] = MigrateOfficialNuGetSource(sources[i]);
            }

            return sources;
        }

        public void SetSources(IEnumerable<string> sources)
        {
            _settingsManager.SetPublishSources(sources);
        }

        public string DefaultSource
        {
            get { return NuGetConstants.NuGetPublishFeed; }
        }

        public string ActiveSource
        {
            get
            {
                return MigrateOfficialNuGetSource(_settingsManager.ActivePublishSource);
            }
            set { _settingsManager.ActivePublishSource = value; }
        }

        #endregion

        private static string MigrateOfficialNuGetSource(string source)
        {
            if (NuGetConstants.V2LegacyNuGetPublishFeed.Equals(source, StringComparison.OrdinalIgnoreCase))
            {
                return NuGetConstants.NuGetPublishFeed;
            }

            return source;
        }
    }
}
