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
            if (newUrl == null) throw new ArgumentNullException(nameof(newUrl));

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
            if (absoluteOrRelativeUri == null) throw new ArgumentNullException(nameof(absoluteOrRelativeUri));

            var uri = absoluteOrRelativeUri.IsAbsoluteUri
                ? absoluteOrRelativeUri
                : new Uri(new Uri(InvokeJS("window.location")), absoluteOrRelativeUri);

            ReplaceUrl(uri.AbsoluteUri);
        }
        public static Uri GetApplicationBaseLocation()
        {
            var location = new Uri(InvokeJS("window.location"));
            return new Uri(location.GetLeftPart(UriPartial.Authority));
        }
    }
}
#endif
