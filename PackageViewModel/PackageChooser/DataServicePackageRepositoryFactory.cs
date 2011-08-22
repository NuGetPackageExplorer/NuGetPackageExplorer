using System;
using System.Collections.Generic;
using NuGet;

namespace PackageExplorerViewModel {
    internal static class DataServicePackageRepositoryFactory {
        private static readonly Dictionary<string, IHttpClient> _httpClientCache = new Dictionary<string, IHttpClient>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static DataServicePackageRepository CreateRepository(string source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            DataServicePackageRepository packageRepository;
            try {
                IHttpClient packageSourceClient;
                if (!_httpClientCache.TryGetValue(source, out packageSourceClient)) {
                    Uri packageUri = new Uri(source, UriKind.Absolute);
                    packageSourceClient = new RedirectedHttpClient(packageUri);

                    _httpClientCache.Add(source, packageSourceClient);
                }
                
                packageRepository = new DataServicePackageRepository(packageSourceClient);
            }
            catch (Exception) {
                packageRepository = null;
            }

            return packageRepository;
        }
    }
}