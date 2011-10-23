using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal class PackageSourceSettings : ISourceSettings
    {
        private ISettingsManager _settingsManager;

        public PackageSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;

            // migrate active package source
            if (ActiveSource.Equals(NuGetConstants.V1FeedUrl, StringComparison.OrdinalIgnoreCase))
            {
                ActiveSource = NuGetConstants.DefaultFeedUrl;
            }
        }

        public IList<string> GetSources()
        {
            var sources = _settingsManager.GetPackageSources();

            // migrate nuget v1 feed to v2 feed
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Equals(NuGetConstants.V1FeedUrl, StringComparison.OrdinalIgnoreCase))
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
            get
            {
                return _settingsManager.ActivePackageSource;
            }
            set
            {
                _settingsManager.ActivePackageSource = value;
            }
        }
    }
}