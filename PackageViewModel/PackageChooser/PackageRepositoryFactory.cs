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
                    .FirstOrDefault(s =>
                    {
                        var configuredUri = s.TrySourceAsUri;
                        return configuredUri != null
                               && Uri.Compare(configuredUri, uri, UriComponents.SchemeAndServer, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0
                               && string.Equals(configuredUri.AbsolutePath.TrimEnd('/'), uri.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal)
                               && string.Equals(configuredUri.Query, uri.Query, StringComparison.Ordinal)
                               && string.Equals(configuredUri.Fragment, uri.Fragment, StringComparison.Ordinal);
                    });

            return CreateRepository(configuredSource ?? new PackageSource(source));
        }
    }


}
