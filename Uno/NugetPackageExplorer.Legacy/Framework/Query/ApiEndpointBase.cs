using System.Diagnostics.CodeAnalysis;

using NupkgExplorer.Framework.Extensions;
using NupkgExplorer.Framework.Json;

namespace NupkgExplorer.Framework.Query
{
    public abstract class ApiEndpointBase
    {
        private readonly HttpClient _client;
        protected HttpClient Client => _client;

        protected ApiEndpointBase()
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(new LoggingHttpClientHandler(handler));

            _client = client;
        }

        protected async Task<HttpResponseMessage> Query(Func<QueryBuilder, QueryBuilder> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            using var query = builder(new QueryBuilder());
            return await query.Query(_client).ConfigureAwait(false);
        }

        protected async Task<HttpResponseMessage> Query(HttpCompletionOption option, Func<QueryBuilder, QueryBuilder> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            using var query = builder(new QueryBuilder());
            return await query.Query(_client, option: option).ConfigureAwait(false);
        }
        protected Task<Json<T>> QueryJson<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<QueryBuilder, QueryBuilder> builder) => Query(builder).ReadAsJson<T>();
        protected Task<JsonArray<T>> QueryJsonArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(Func<QueryBuilder, QueryBuilder> builder) => Query(builder).ReadAsJsonArray<T>();
    }
}
