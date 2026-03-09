#if __WASM__
using System;
using System.Collections.Generic;
using System.Text;

using static Uno.Foundation.WebAssemblyRuntime;

namespace NuGetPackageExplorer.Helpers
{
    public static class ApplicationHelper
    {
        /// <summary>
        /// Update url without refreshing the page
        /// </summary>
        /// <param name="newUrl">new url</param>
        /// <remarks>The new URL must be of the same origin as the current URL; otherwise replaceState throws an exception.</remarks>
        public static void ReplaceUrl(string newUrl)
        {
            ArgumentNullException.ThrowIfNull(newUrl);

            var js = @$"
            window.history.replaceState(
                window.history.state,
                document.title,
                ""{EscapeJs(newUrl)}""
            )";
            InvokeJS(js);
        }

        /// <summary>
        /// Update url without refresh the page
        /// </summary>
        /// <param name="absoluteOrRelativeUri">new url</param>
        public static void ReplaceUrl(Uri absoluteOrRelativeUri)
        {
            ArgumentNullException.ThrowIfNull(absoluteOrRelativeUri);

            var uri = absoluteOrRelativeUri.IsAbsoluteUri
                ? absoluteOrRelativeUri
                : new Uri(GetApplicationBaseLocation(), absoluteOrRelativeUri.OriginalString.TrimStart('/'));

            ReplaceUrl(uri.AbsoluteUri);
        }
        public static Uri GetApplicationBaseLocation()
        {
            var baseLocation = InvokeJS(
                """
                (() => {
                    const resolutionBase = document.baseURI || window.location.href;
                    const baseElement = document.querySelector('base[href]');
                    let basePath = '';

                    if (baseElement) {
                        try {
                            const baseUrl = new URL(baseElement.getAttribute('href'), resolutionBase);
                            basePath = baseUrl.pathname || '';
                        } catch {
                        }
                    }

                    if (!basePath) {
                        const bootstrapScript = document.querySelector('script[src*="uno-bootstrap.js"]');
                        const scriptSource = bootstrapScript?.getAttribute('src') ?? '/';
                        const scriptUrl = new URL(scriptSource, resolutionBase);
                        const scriptPath = scriptUrl.pathname || '/';
                        const packageMarkerIndex = scriptPath.indexOf('/package_');

                        if (packageMarkerIndex >= 0) {
                            basePath = scriptPath.substring(0, packageMarkerIndex);
                        } else {
                            const lastSlash = scriptPath.lastIndexOf('/');
                            basePath = lastSlash > 0 ? scriptPath.substring(0, lastSlash) : '/';
                        }
                    }

                    if (!basePath) {
                        basePath = '/';
                    }

                    if (!basePath.endsWith('/')) {
                        basePath += '/';
                    }

                    return new URL(basePath, window.location.origin).toString();
                })()
                """
            );

            return new Uri(baseLocation);
        }
    }
}
#endif
