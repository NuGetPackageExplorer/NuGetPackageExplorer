using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NupkgExplorer.Framework.Json
{
	public class Json<T> : JObject
	{
		public T Content { get; }

		public Json() { }
		public Json(string json) : base(JObject.Parse(json))
		{
			Content = ToObject<T>();
		}
		public static new Json<T> Parse(string json) => new Json<T>(json);

		private object ToDump() => Content;
		public override string ToString()
		{
			// hide default ToString
			return typeof(T).GetMethod(nameof(ToString)).DeclaringType != typeof(object)
				? Content.ToString()
				: string.Empty;
		}
	}
}
