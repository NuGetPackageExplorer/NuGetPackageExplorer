using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace PackageExplorerViewModel
{
    public static class PackageRepositoryFactory
    {
        public static SourceRepository CreateRepository(PackageSource packageSource, IEnumerable<Lazy<INuGetResourceProvider>>? additionalProviders)
        {
            var providers = Repository.Provider.GetCoreV3();

            if (additionalProviders != null)
            {
                providers = providers.Concat(additionalProviders);
            }

            return Repository.CreateSource(providers, packageSource);
        }
        public static SourceRepository CreateRepository(PackageSource packageSource) => CreateRepository(packageSource, null);

        public static SourceRepository CreateRepository(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            Uri uri;
            try
            {

                uri = new Uri(source);
            }
            catch (UriFormatException)
            {
                throw new ArgumentException("Invalid URL", nameof(source));
            }

            var settings = Settings.LoadDefaultSettings(null);
            var configuredSource =
                SettingsUtility
                    .GetEnabledSources(settings)
                    .FirstOrDefault(s => string.Equals(s.Source.TrimEnd('/'), source.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));

            return CreateRepository(configuredSource ?? new PackageSource(source));
        }
    }


}
