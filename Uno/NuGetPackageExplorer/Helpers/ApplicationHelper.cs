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
                    const bootstrapScript = document.querySelector('script[src*="uno-bootstrap.js"]');
                    const scriptSource = bootstrapScript?.getAttribute('src') ?? '/';
                    const packageMarkerIndex = scriptSource.indexOf('/package_');
                    const basePath = packageMarkerIndex > 0
                        ? scriptSource.substring(0, packageMarkerIndex)
                        : '';

                    return new URL(basePath ? `${basePath}/` : '/', window.location.origin).toString();
                })()
                """
            );

            return new Uri(baseLocation);
        }
    }
}
#endif
