using System;
using System.Collections.Generic;
using System.Linq;

using NuGet.Packaging;

namespace NuGetPe
{
    public static class CollectionExtensions
    {
        public static void CopyTo<T>(this IEnumerable<T> sourceCollection, ICollection<T> targetCollection)
        {
            ArgumentNullException.ThrowIfNull(sourceCollection);
            ArgumentNullException.ThrowIfNull(targetCollection);

            targetCollection.Clear();
            targetCollection.AddRange(sourceCollection);
        }

        public static int RemoveAll<T>(this ICollection<T> collection, Func<T, bool> match)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(match);

            List<T> toRemove = [.. collection.Where(match)];
            foreach (var item in toRemove)
            {
                collection.Remove(item);
            }
            return toRemove.Count;
        }

        public static IEnumerable<T?> CastAsNullable<T>(this IEnumerable<T> source) where T : struct
        {
            return source.Cast<T?>();
        }
    }
}
