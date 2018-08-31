using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;

namespace PackageExplorerViewModel
{
    internal static class PathUtility
    {
        /// <summary>
        /// Creates a relative path from one file
        /// or folder to another.
        /// </summary>
        /// <param name="fromDirectory">
        /// Contains the directory that defines the
        /// start of the relative path.
        /// </param>
        /// <param name="toPath">
        /// Contains the path that defines the
        /// endpoint of the relative path.
        /// </param>
        /// <returns>
        /// The relative path from the start
        /// directory to the end path.
        /// </returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string RelativePathTo(string fromDirectory, string toPath)
        {
            if (fromDirectory == null)
            {
                throw new ArgumentNullException("fromDirectory");
            }

            if (toPath == null)
            {
                throw new ArgumentNullException("toPath");
            }

            var isRooted = Path.IsPathRooted(fromDirectory)
                && Path.IsPathRooted(toPath);

            if (isRooted)
            {
                var isDifferentRoot = !string.Equals(
                    Path.GetPathRoot(fromDirectory),
                    Path.GetPathRoot(toPath), StringComparison.OrdinalIgnoreCase);

                if (isDifferentRoot)
                {
                    return toPath;
                }
            }

            var relativePath = new StringCollection();
            var fromDirectories = fromDirectory.Split(
                Path.DirectorySeparatorChar);

            var toDirectories = toPath.Split(
                Path.DirectorySeparatorChar);

            var length = Math.Min(
                fromDirectories.Length,
                toDirectories.Length);

            var lastCommonRoot = -1;

            // find common root
            for (var x = 0; x < length; x++)
            {
                if (!string.Equals(fromDirectories[x], toDirectories[x], StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                lastCommonRoot = x;
            }

            if (lastCommonRoot == -1)
            {
                return toPath;
            }

            // add relative folders in from path
            for (var x = lastCommonRoot + 1; x < fromDirectories.Length; x++)
            {
                if (fromDirectories[x].Length > 0)
                {
                    relativePath.Add("..");
                }
            }

            // add to folders to path
            for (var x = lastCommonRoot + 1; x < toDirectories.Length; x++)
            {
                relativePath.Add(toDirectories[x]);
            }

            // create relative path
            var relativeParts = new string[relativePath.Count];
            relativePath.CopyTo(relativeParts, 0);

            var newPath = string.Join(
                Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture),
                relativeParts);

            return newPath;
        }
    }
}
