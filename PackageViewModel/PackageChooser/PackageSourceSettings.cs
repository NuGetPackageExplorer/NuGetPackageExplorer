using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal class PackageSourceSettings : ISourceSettings
    {
        private readonly ISettingsManager _settingsManager;

        public PackageSourceSettings(ISettingsManager settingsManager)
        {
            Debug.Assert(settingsManager != null);
            _settingsManager = settingsManager;

            if (settingsManager.IsFirstTimeAfterUpdate)
            {
                // migrate active package source
                if (ActiveSource.Equals(NuGetConstants.V2LegacyFeedUrl, StringComparison.OrdinalIgnoreCase))
                {
                    ActiveSource = NuGetConstants.DefaultFeedUrl;
                }
            }
        }

        #region ISourceSettings Members

        public IList<string> GetSources()
        {
            IList<string> sources = _settingsManager.GetPackageSources();

            if (_settingsManager.IsFirstTimeAfterUpdate)
            {
                // migrate nuget v1 feed to v2 feed
                for (int i = 0; i < sources.Count; i++)
                {
                    if (sources[i].Equals(NuGetConstants.V2LegacyFeedUrl, StringComparison.OrdinalIgnoreCase))
                    {
                        sources[i] = NuGetConstants.DefaultFeedUrl;
                    }
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