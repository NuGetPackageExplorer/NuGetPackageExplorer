using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NupkgExplorer.Framework.Json;
using Uno.Extensions;
using Uno.Logging;

namespace NupkgExplorer.Framework.Extensions
{
	internal static class HttpClientExtensions
	{
        public static async Task<HttpResponseMessage> EnsureSuccessStatusCode(this Task<HttpResponseMessage> task, Func<HttpResponseMessage, object> contextProvider = null)
        {
            var response = await task;

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpResponseExceptionWithStatusCode(
                    response.StatusCode,
                    $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            return response;
        }

		public static async Task<Stream> ReadAsStreamAsync(this Task<HttpResponseMessage> task)
		{
			var response = await task;

			return await response.Content.ReadAsStreamAsync();
		}

		public static async Task<JObject> ReadAsJObject(this Task<HttpResponseMessage> task)
		{
			var response = await task;
			var content = await response.Content.ReadAsStringAsync();

			try
			{
				return JObject.Parse(content);
			}
			catch (Exception e)
			{
				typeof(HttpResponseMessage).Log().Error("Failed to parse response content", e);
				throw;
			}
		}
		public static async Task<Json<T>> ReadAsJson<T>(this Task<HttpResponseMessage> task)
		{
			var response = await task;
			var content = await response.Content.ReadAsStringAsync();

			try
			{
				return Json<T>.Parse(content);
			}
			catch (Exception e)
			{
				typeof(HttpResponseMessage).Log().Error("Failed to parse response content", e);
				throw;
			}
		}
		public static async Task<JsonArray<T>> ReadAsJsonArray<T>(this Task<HttpResponseMessage> task)
		{
			var response = await task;
			var content = await response.Content.ReadAsStringAsync();

			try
			{
				return JsonArray<T>.Parse(content);
			}
			catch (Exception e)
			{
				typeof(HttpResponseMessage).Log().Error("Failed to parse response content", e);
				throw;
			}
		}
	}
}
