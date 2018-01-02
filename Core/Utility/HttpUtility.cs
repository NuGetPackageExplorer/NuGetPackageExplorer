using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;

namespace NuGetPe
{
    public static class HttpUtility
    {
        private const string UserAgentTemplate = "{0}/{1} ({2})";

        public static string CreateUserAgentString(string client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            var version = typeof(HttpUtility).Assembly.GetNameSafe().Version;
            return string.Format(CultureInfo.InvariantCulture, UserAgentTemplate, client, version, Environment.OSVersion);
        }

        public static void SetUserAgent(WebRequest request, string userAgent)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (userAgent == null)
            {
                throw new ArgumentNullException("userAgent");
            }

            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null)
            {
                httpRequest.UserAgent = userAgent;
            }
            else
            {
                httpRequest.Headers[HttpRequestHeader.UserAgent] = userAgent;
            }
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            var result = new NameValueCollection();
            if (!string.IsNullOrEmpty(query))
            {
                if (query.StartsWith("?", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Substring(1);
                }

                var parts = query.Split('&', '=');
                for (var i = 0; i < parts.Length; i += 2)
                {
                    var name = parts[i];
                    var value = (i + 1 < parts.Length) ? parts[i + 1] : null;
                    result[name] = value;
                }
            }

            return result;
        }
    }
}