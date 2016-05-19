using System;
using System.Collections.Generic;
using System.Linq;

namespace NuGetPe
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                collection.Add(item);
            }
        }

        public static void CopyTo<T>(this IEnumerable<T> sourceCollection, ICollection<T> targetCollection)
        {
            targetCollection.Clear();
            targetCollection.AddRange(sourceCollection);
        }

        public static int RemoveAll<T>(this ICollection<T> collection, Func<T, bool> match)
        {
            IList<T> toRemove = collection.Where(match).ToList();
            foreach (T item in toRemove)
            {
                collection.Remove(item);
            }
            return toRemove.Count;
        }
    }
}