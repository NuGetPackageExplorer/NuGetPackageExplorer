using System;
using System.Collections.Concurrent;
using System.Net;

namespace NuGet {
    internal class ProxyCache : IProxyCache {
        /// <summary>
        /// Capture the default System Proxy so that it can be re-used by the IProxyFinder
        /// because we can't rely on WebRequest.DefaultWebProxy since someone can modify the DefaultWebProxy
        /// property and we can't tell if it was modified and if we are still using System Proxy Settings or not.
        /// One limitation of this method is that it does not look at the config file to get the defined proxy
        /// settings.
        /// </summary>
        private static readonly IWebProxy _originalSystemProxy = WebRequest.GetSystemWebProxy();

        private readonly ConcurrentDictionary<Uri, WebProxy> _cache = new ConcurrentDictionary<Uri, WebProxy>();

        private static readonly ProxyCache _instance = new ProxyCache();

        internal static ProxyCache Instance {
            get {
                return _instance;
            }
        }

        public IWebProxy GetProxy(Uri uri) {
            if (!IsSystemProxySet(uri)) {
                return null;
            }

            WebProxy systemProxy = GetSystemProxy(uri);

            WebProxy effectiveProxy;
            // See if we have a proxy instance cached for this proxy address
            if (_cache.TryGetValue(systemProxy.Address, out effectiveProxy)) {
                return effectiveProxy;
            }

            return systemProxy;
        }

        public void Add(IWebProxy proxy) {
            var webProxy = proxy as WebProxy;
            if (webProxy != null) {
                _cache.TryAdd(webProxy.Address, webProxy);
            }
        }

        private static WebProxy GetSystemProxy(Uri uri) {
            // WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
            // proxy settings instead of the WebRequest.GetSystemProxy()
            var proxyUri = _originalSystemProxy.GetProxy(uri);
            return new WebProxy(proxyUri);
        }

        /// <summary>
        /// Return true or false if connecting through a proxy server
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        private static bool IsSystemProxySet(Uri uri) {
            // The reason for not calling the GetSystemProxy is because the object
            // that will be returned is no longer going to be the proxy that is set by the settings
            // on the users machine only the Address is going to be the same.
            // Not sure why the .NET team did not want to expose all of the usefull settings like
            // ByPass list and other settings that we can't get because of it.
            // Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
            // getting the proxy for to should be bypassed or not. If it should be bypassed then
            // return that we don't need a proxy and we should try to connect directly.
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            if (proxy != null) {
                Uri proxyAddress = new Uri(proxy.GetProxy(uri).AbsoluteUri);
                if (String.Equals(proxyAddress.AbsoluteUri, uri.AbsoluteUri)) {
                    return false;
                }
                bool bypassUri = proxy.IsBypassed(uri);
                if (bypassUri) {
                    return false;
                }
                proxy = new WebProxy(proxyAddress);
            }

            return proxy != null;
        }
    }

}
