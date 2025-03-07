using NupkgExplorer.Framework.Extensions;

using Uno.Extensions;

namespace NupkgExplorer.Framework.Query
{
    public class QueryBuilder
    {
        private readonly HttpRequestMessage _request = new();
        private readonly Dictionary<string, string> _query = [];
        private readonly Dictionary<string, string> _payload = [];

        public QueryBuilder Get() => Do(() => _request.Method = HttpMethod.Get);
        public QueryBuilder Put() => Do(() => _request.Method = HttpMethod.Put);
        public QueryBuilder Put(string contentType /*ignored*/) => Do(() => _request.Method = HttpMethod.Put);
        public QueryBuilder Post() => Do(() => _request.Method = HttpMethod.Post);
        public QueryBuilder PostMultipart() => throw new NotImplementedException();
        public QueryBuilder Part(string name, Stream stream, string mimeType, string filename, Func<bool> condition) => throw new NotImplementedException();
        public QueryBuilder Delete() => Do(() => _request.Method = HttpMethod.Delete);

        public QueryBuilder AppendPath(string path) => Do(() => _request.RequestUri = new Uri(path, UriKind.Relative));
        public QueryBuilder FromUrl(string nextUrl) => Do(() => _request.RequestUri = new Uri(nextUrl));

        public QueryBuilder Param(string name, string value) => Do(() => _query.Add(name, value));
        public QueryBuilder Param(string name, string value, Func<bool> condition) => Do(() => _query.Add(name, value), condition);
        public QueryBuilder Param(string name, Func<string> value, Func<bool> condition) => Do(() => _query.Add(name, value()), condition);

        public QueryBuilder PayloadParam(string name, string value) => Do(() => _payload.Add(name, value));
        public QueryBuilder PayloadParam(string name, string value, Func<bool> condition) => Do(() => _payload.Add(name, value), condition);
        public QueryBuilder PayloadParam(string name, Func<string> value, Func<bool> condition) => Do(() => _payload.Add(name, value()), condition);

        private QueryBuilder Do(Action action)
        {
            action();
            return this;
        }
        private QueryBuilder Do(Action action, Func<bool> condition)
        {
            if (condition()) action();
            return this;
        }

        public Task<HttpResponseMessage> Query(HttpClient httpClient, bool ensureSuccess = true, HttpCompletionOption option = HttpCompletionOption.ResponseContentRead)
        {
            if (_query.Any() && _payload.Any())
                throw new InvalidOperationException("Param and PayloadParam should not be used together");

            if (_query.Any())
                _request.RequestUri = new Uri(_request.RequestUri!.OriginalString + "?" + GetQueryString(), _request.RequestUri!.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
            if (_payload.Any())
                _request.Content = new FormUrlEncodedContent(_payload);

            return httpClient
                .SendAsync(_request, option)
                .Apply(x => ensureSuccess ? x.EnsureSuccessStatusCode() : x);

            string GetQueryString(bool filterEmptyValue = true, bool addQueryIndicator = false)
            {
                using (var content = new FormUrlEncodedContent(_query.Where(x => !filterEmptyValue || !string.IsNullOrEmpty(x.Value))))
                {
                    var query = content.ReadAsStringAsync().Result;
                    if (addQueryIndicator && !string.IsNullOrEmpty(query))
                        query = "?" + query;

                    return query;
                }
                ;
            }
        }
    }
}
