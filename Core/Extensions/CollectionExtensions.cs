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
            if (sourceCollection is null)
                throw new ArgumentNullException(nameof(sourceCollection));
            if (targetCollection is null)
                throw new ArgumentNullException(nameof(targetCollection));

            targetCollection.Clear();
            targetCollection.AddRange(sourceCollection);
        }

        public static int RemoveAll<T>(this ICollection<T> collection, Func<T, bool> match)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection));
            if (match is null)
                throw new ArgumentNullException(nameof(match));

            IList<T> toRemove = collection.Where(match).ToList();
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
