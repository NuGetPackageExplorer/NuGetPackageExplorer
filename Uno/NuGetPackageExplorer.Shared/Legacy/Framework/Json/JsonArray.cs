using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NupkgExplorer.Framework.Json
{

	public class JsonArray<T> : JArray
	{
		public T[] Items { get; }

		public JsonArray() { }
		public JsonArray(string json) : base(JArray.Parse(json))
		{
			Items = JsonConvert.DeserializeObject<T[]>(json);
		}
		public static new JsonArray<T> Parse(string json) => new JsonArray<T>(json);

		private object ToDump() => Items;
		public override string ToString()
		{
			// hide default ToString
			return typeof(T).GetMethod(nameof(ToString)).DeclaringType != typeof(object)
				? Items.ToString()
				: string.Empty;
		}
	}
}
