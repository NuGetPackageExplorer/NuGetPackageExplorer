using System.Diagnostics.CodeAnalysis;

using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Json;

namespace NupkgExplorer.Framework.Query
{
    public abstract class ApiEndpointBase
    {
        private readonly HttpClient _client;

        public ApiEndpointBase()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(new LoggingHttpClientHandler(handler));

            _client = client;
        }

        protected Task<HttpResponseMessage> Query(Func<QueryBuilder, QueryBuilder> builder) => builder(new QueryBuilder()).Query(_client);
        protected Task<HttpResponseMessage> Query(HttpCompletionOption option, Func<QueryBuilder, QueryBuilder> builder) => builder(new QueryBuilder()).Query(_client, option: option);
        protected Task<Json<T>> QueryJson<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<QueryBuilder, QueryBuilder> builder) => Query(builder).ReadAsJson<T>();
        protected Task<JsonArray<T>> QueryJsonArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<QueryBuilder, QueryBuilder> builder) => Query(builder).ReadAsJsonArray<T>();
    }
}
