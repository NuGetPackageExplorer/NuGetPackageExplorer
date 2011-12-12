using System;
using System.Collections.Generic;
using System.Diagnostics;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal class PublishSourceSettings : ISourceSettings
    {
        private const string V1NuGetPublishFeed = "http://go.microsoft.com/fwlink/?LinkID=207106";
        private const string NuGetPublishFeed = "https://nuget.org";

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
            for (int i = 0; i < sources.Count; i++)
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
            get { return NuGetPublishFeed; }
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
            if (V1NuGetPublishFeed.Equals(source, StringComparison.OrdinalIgnoreCase))
            {
                return NuGetPublishFeed;
            }

            return source;
        }
    }
}