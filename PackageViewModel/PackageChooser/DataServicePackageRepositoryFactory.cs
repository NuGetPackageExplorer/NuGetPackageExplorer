using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using NuGet;

namespace PackageExplorerViewModel {
    internal class DataServicePackageRepositoryFactory {
        private readonly IProxyService _proxyService;
        private static readonly Dictionary<string, IHttpClient> _httpClientCache = new Dictionary<string, IHttpClient>();

        public DataServicePackageRepositoryFactory(IProxyService proxyService) {
            Debug.Assert(proxyService != null);            
            _proxyService = proxyService;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public DataServicePackageRepository CreateRepository(string source) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }

            DataServicePackageRepository packageRepository;
            try {
                IHttpClient packageSourceClient;
                if (!_httpClientCache.TryGetValue(source, out packageSourceClient)) {
                    Uri packageUri = new Uri(source, UriKind.Absolute);
                    IWebProxy packageSourceProxy = _proxyService.GetProxy(packageUri);
                    packageSourceClient = new RedirectedHttpClient(packageUri, packageSourceProxy);

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