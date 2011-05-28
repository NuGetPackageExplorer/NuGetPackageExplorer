using System;
using System.Globalization;
using System.Net;

namespace NuGet {
    public static class HttpUtility {

        private const string UserAgentTemplate = "{0}/{1} ({2})";

        public static string CreateUserAgentString(string client) {
            if (client == null) {
                throw new ArgumentNullException("client");
            }

            var version = typeof(HttpUtility).Assembly.GetNameSafe().Version;
            return String.Format(CultureInfo.InvariantCulture, UserAgentTemplate, client, version, Environment.OSVersion);
        }

        public static void SetUserAgent(WebRequest request, string userAgent) {
            if (request == null) {
                throw new ArgumentNullException("request");
            }

            if (userAgent == null) {
                throw new ArgumentNullException("userAgent");
            }

            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) {
                httpRequest.UserAgent = userAgent;
            }
            else {
                httpRequest.Headers[HttpRequestHeader.UserAgent] = userAgent;
            }
        }
    }
}
