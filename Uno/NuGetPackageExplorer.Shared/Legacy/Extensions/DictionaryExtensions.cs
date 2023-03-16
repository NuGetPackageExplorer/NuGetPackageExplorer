using System;
using System.Collections.Generic;
using System.Text;

namespace NupkgExplorer.Extensions
{
	public static class DictionaryExtensions
	{
		public static bool TryGetOrAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory, out TValue result)
		{
			if (dict.TryGetValue(key, out result)) return true;

			result = valueFactory(key);
			dict[key] = result;
			return false;
		}
		public static TValue GetOrAddValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> valueFactory)
		{
			dict.TryGetOrAddValue(key, valueFactory, out var result);
			return result;
		}
	}
}
