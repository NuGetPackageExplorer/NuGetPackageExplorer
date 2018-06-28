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
            targetCollection.Clear();
            targetCollection.AddRange(sourceCollection);
        }

        public static int RemoveAll<T>(this ICollection<T> collection, Func<T, bool> match)
        {
            IList<T> toRemove = collection.Where(match).ToList();
            foreach (var item in toRemove)
            {
                collection.Remove(item);
            }
            return toRemove.Count;
        }
    }
}