using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal class PackageSourceSettings : ISourceSettings
    {
        private readonly ISettingsManager _settingsManager;

        public PackageSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;

            // migrate active package source
            if (NuGetConstants.V2FeedUrl.Equals(ActiveSource, StringComparison.OrdinalIgnoreCase) ||
                NuGetConstants.V2LegacyFeedUrl.Equals(ActiveSource, StringComparison.OrdinalIgnoreCase))
            {
                ActiveSource = NuGetConstants.DefaultFeedUrl;
            }
        }

        #region ISourceSettings Members

        public IList<string> GetSources()
        {
            var sources = _settingsManager.GetPackageSources();


            // migrate nuget v1 feed to v2 feed
            for (var i = 0; i < sources.Count; i++)
            {
                if (sources[i].Equals(NuGetConstants.V2LegacyFeedUrl, StringComparison.OrdinalIgnoreCase) ||
                    sources[i].Equals(NuGetConstants.V2FeedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    sources[i] = NuGetConstants.DefaultFeedUrl;
                }
            }

            return sources;
        }

        public void SetSources(IEnumerable<string> sources)
        {
            _settingsManager.SetPackageSources(sources);
        }

        public string DefaultSource
        {
            get { return NuGetConstants.DefaultFeedUrl; }
        }

        public string ActiveSource
        {
            get { return _settingsManager.ActivePackageSource; }
            set { _settingsManager.ActivePackageSource = value; }
        }

        #endregion
    }
}
