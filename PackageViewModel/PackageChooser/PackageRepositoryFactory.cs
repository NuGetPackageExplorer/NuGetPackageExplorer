using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NuGet;

namespace PackageExplorerViewModel
{
    internal static class PackageRepositoryFactory
    {
        private static readonly Dictionary<string, IHttpClient> _httpClientCache = new Dictionary<string, IHttpClient>();

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static IPackageRepository CreateRepository(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var uri = new Uri(source);
            if (uri.IsFile)
            {
                return new LocalPackageRepository(source);
            }

            IHttpClient packageSourceClient;
            if (!_httpClientCache.TryGetValue(source, out packageSourceClient))
            {
                var packageUri = new Uri(source, UriKind.Absolute);
                packageSourceClient = new RedirectedHttpClient(packageUri);

                _httpClientCache.Add(source, packageSourceClient);
            }

            return new DataServicePackageRepository(packageSourceClient);
        }
    }
}