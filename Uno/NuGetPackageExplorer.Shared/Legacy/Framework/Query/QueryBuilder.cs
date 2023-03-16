using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NupkgExplorer.Framework.Extensions;

using Uno.Extensions;

namespace NupkgExplorer.Framework.Query
{
	public class QueryBuilder
	{
		private HttpRequestMessage request = new HttpRequestMessage();
		private Dictionary<string, string> query = new Dictionary<string, string>();
		private Dictionary<string, string> payload = new Dictionary<string, string>();

		public QueryBuilder Get() => Do(() => request.Method = HttpMethod.Get);
		public QueryBuilder Put() => Do(() => request.Method = HttpMethod.Put);
		public QueryBuilder Put(string contentType /*ignored*/) => Do(() => request.Method = HttpMethod.Put);
		public QueryBuilder Post() => Do(() => request.Method = HttpMethod.Post);
		public QueryBuilder PostMultipart() => throw new NotImplementedException();
		public QueryBuilder Part(string name, Stream stream, string mimeType, string filename, Func<bool> condition) => throw new NotImplementedException();
		public QueryBuilder Delete() => Do(() => request.Method = HttpMethod.Delete);

		public QueryBuilder AppendPath(string path) => Do(() => request.RequestUri = new Uri(path, UriKind.Relative));
		public QueryBuilder FromUrl(string nextUrl) => Do(() => request.RequestUri = new Uri(nextUrl));

		public QueryBuilder Param(string name, string value) => Do(() => query.Add(name, value));
		public QueryBuilder Param(string name, string value, Func<bool> condition) => Do(() => query.Add(name, value), condition);
		public QueryBuilder Param(string name, Func<string> value, Func<bool> condition) => Do(() => query.Add(name, value()), condition);

		public QueryBuilder PayloadParam(string name, string value) => Do(() => payload.Add(name, value));
		public QueryBuilder PayloadParam(string name, string value, Func<bool> condition) => Do(() => payload.Add(name, value), condition);
		public QueryBuilder PayloadParam(string name, Func<string> value, Func<bool> condition) => Do(() => payload.Add(name, value()), condition);

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
			if (query.Any() && payload.Any())
				throw new InvalidOperationException("Param and PayloadParam should not be used togather");

			if (query.Any())
				request.RequestUri = new Uri(request.RequestUri.OriginalString + "?" + GetQueryString(), request.RequestUri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);
			if (payload.Any())
				request.Content = new FormUrlEncodedContent(payload);

			return httpClient
				.SendAsync(request, option)
				.Apply(x => ensureSuccess ? x.EnsureSuccessStatusCode() : x);

			string GetQueryString(bool filterEmptyValue = true, bool addQueryIndicator = false)
			{
				using (var content = new FormUrlEncodedContent(query.Where(x => !filterEmptyValue || !string.IsNullOrEmpty(x.Value))))
				{
					var query = content.ReadAsStringAsync().Result;
					if (addQueryIndicator && !string.IsNullOrEmpty(query))
						query = "?" + query;

					return query;
				};
			}
		}
	}
}
