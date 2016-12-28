using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NuGetPe
{
    public static class PathResolver
    {
        /// <summary>
        /// Returns a collection of files from the source that matches the wildcard.
        /// </summary>
        /// <param name="source">The collection of files to match.</param>
        /// <param name="getPath">Function that returns the path to filter a package file </param>
        /// <param name="wildcards">The wildcard to apply to match the path with.</param>
        /// <returns></returns>
        public static IEnumerable<T> GetMatches<T>(IEnumerable<T> source, Func<T, string> getPath,
                                                   IEnumerable<string> wildcards) where T : IPackageFile
        {
            IEnumerable<Regex> filters = wildcards.Select(WildcardToRegex);
            return source.Where(item =>
                                {
                                    string path = getPath(item);
                                    return filters.Any(f => f.IsMatch(path));
                                });
        }

        /// <summary>
        /// Removes files from the source that match any wildcard.
        /// </summary>
        public static void FilterPackageFiles<T>(ICollection<T> source, Func<T, string> getPath,
                                                 IEnumerable<string> wildcards) where T : IPackageFile
        {
            var matchedFiles = new HashSet<T>(GetMatches(source, getPath, wildcards));
            source.RemoveAll(matchedFiles.Contains);
        }

        public static string NormalizeWildcard(string basePath, string wildcard)
        {
            basePath = NormalizeBasePath(basePath, ref wildcard);
            return Path.Combine(basePath, wildcard);
        }

        private static Regex WildcardToRegex(string wildcard)
        {
            return new Regex('^'
                             + Regex.Escape(wildcard)
                                   .Replace(@"\*\*\\", ".*")
                //For recursive wildcards \**\, include the current directory.
                                   .Replace(@"\*\*", ".*")
                // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                                   .Replace(@"\*", @"[^\\]*(\\)?")
                // For non recursive searches, limit it any character that is not a directory separator
                                   .Replace(@"\?", ".") // ? translates to a single any character
                             + '$', RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        }

        internal static IEnumerable<PackageFileBase> ResolveSearchPattern(string basePath, string searchPath, string targetPath)
        {
            if (!searchPath.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
            {
                // If we aren't dealing with network paths, trim the leading slash. 
                searchPath = searchPath.TrimStart(Path.DirectorySeparatorChar);
            }

            bool searchDirectory = false;

            // If the searchPath ends with \ or /, we treat searchPath as a directory,   
            // and will include everything under it, recursively   
            if (IsDirectoryPath(searchPath))
            {
                searchPath = searchPath + "**" + Path.DirectorySeparatorChar + "*";
                searchDirectory = true;
            }

            basePath = NormalizeBasePath(basePath, ref searchPath);
            string basePathToEnumerate = GetPathToEnumerateFrom(basePath, searchPath);

            // Append the basePath to searchPattern and get the search regex. We need to do this because the search regex is matched from line start.
            Regex searchRegex = WildcardToRegex(Path.Combine(basePath, searchPath));

            // This is a hack to prevent enumerating over the entire directory tree if the only wildcard characters are the ones in the file name. 
            // If the path portion of the search path does not contain any wildcard characters only iterate over the TopDirectory.
            SearchOption searchOption = SearchOption.AllDirectories;
            // (a) Path is not recursive search
            bool isRecursiveSearch = searchPath.IndexOf("**", StringComparison.OrdinalIgnoreCase) != -1;
            // (b) Path does not have any wildcards.
            bool isWildcardPath = Path.GetDirectoryName(searchPath).Contains('*');
            if (!isRecursiveSearch && !isWildcardPath)
            {
                searchOption = SearchOption.TopDirectoryOnly;
            }

            // Starting from the base path, enumerate over all files and match it using the wildcard expression provided by the user.
            IEnumerable<string> files = Directory.EnumerateFiles(basePathToEnumerate, "*.*", searchOption);
            IEnumerable<PackageFileBase> matchedFiles =
                from file in files
                where searchRegex.IsMatch(file)
                let targetPathInPackage = ResolvePackagePath(basePathToEnumerate, searchPath, file, targetPath)
                select new PhysicalPackageFile(isTempFile: false, originalPath: file, targetPath: targetPathInPackage);

            if (searchDirectory && IsEmptyDirectory(basePathToEnumerate))
            {
                matchedFiles = matchedFiles.Concat(new[] { new EmptyFolderFile(targetPath ?? String.Empty) });
            }

            return matchedFiles;
        }

        internal static string GetPathToEnumerateFrom(string basePath, string searchPath)
        {
            string basePathToEnumerate;
            int wildcardIndex = searchPath.IndexOf('*');
            if (wildcardIndex == -1)
            {
                // For paths without wildcard, we could either have base relative paths (such as lib\foo.dll) or paths outside the base path
                // (such as basePath: C:\packages and searchPath: D:\packages\foo.dll)
                // In this case, Path.Combine would pick up the right root to enumerate from.
                string searchRoot = Path.GetDirectoryName(searchPath);
                basePathToEnumerate = Path.Combine(basePath, searchRoot);
            }
            else
            {
                // If not, find the first directory separator and use the path to the left of it as the base path to enumerate from.
                int directorySeparatoryIndex = searchPath.LastIndexOf(Path.DirectorySeparatorChar, wildcardIndex);
                if (directorySeparatoryIndex == -1)
                {
                    // We're looking at a path like "NuGet*.dll", NuGet*\bin\release\*.dll
                    // In this case, the basePath would continue to be the path to begin enumeration from.
                    basePathToEnumerate = basePath;
                }
                else
                {
                    string nonWildcardPortion = searchPath.Substring(0, directorySeparatoryIndex);
                    basePathToEnumerate = Path.Combine(basePath, nonWildcardPortion);
                }
            }
            return basePathToEnumerate;
        }

        /// <summary>
        /// Determins the path of the file inside a package.
        /// For recursive wildcard paths, we preserve the path portion beginning with the wildcard.
        /// For non-recursive wildcard paths, we use the file name from the actual file path on disk.
        /// </summary>
        internal static string ResolvePackagePath(string searchDirectory, string searchPattern, string fullPath,
                                                  string targetPath)
        {
            string packagePath;
            bool isDirectorySearch = IsDirectoryPath(searchPattern);
            bool isWildcardSearch = IsWildcardSearch(searchPattern);
            bool isRecursiveWildcardSearch = isWildcardSearch &&
                                             searchPattern.IndexOf("**", StringComparison.OrdinalIgnoreCase) != -1;

            if ((isRecursiveWildcardSearch || isDirectorySearch) && fullPath.StartsWith(searchDirectory, StringComparison.OrdinalIgnoreCase))
            {
                // The search pattern is recursive. Preserve the non-wildcard portion of the path.
                // e.g. Search: X:\foo\**\*.cs results in SearchDirectory: X:\foo and a file path of X:\foo\bar\biz\boz.cs
                // Truncating X:\foo\ would result in the package path.
                packagePath = fullPath.Substring(searchDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            }
            else if (!isWildcardSearch &&
                     Path.GetExtension(searchPattern).Equals(Path.GetExtension(targetPath),
                                                             StringComparison.OrdinalIgnoreCase))
            {
                // If the search does not contain wild cards, and the target path shares the same extension, copy it
                // e.g. <file src="ie\css\style.css" target="Content\css\ie.css" /> --> Content\css\ie.css
                return targetPath;
            }
            else
            {
                packagePath = Path.GetFileName(fullPath);
            }
            return Path.Combine(targetPath ?? String.Empty, packagePath);
        }

        internal static string NormalizeBasePath(string basePath, ref string searchPath)
        {
            const string relativePath = @"..\";

            // If no base path is provided, use the current directory.
            basePath = String.IsNullOrEmpty(basePath) ? @".\" : basePath;

            // If the search path is relative, transfer the ..\ portion to the base path. 
            // This needs to be done because the base path determines the root for our enumeration.
            while (searchPath.StartsWith(relativePath, StringComparison.OrdinalIgnoreCase))
            {
                basePath = Path.Combine(basePath, relativePath);
                searchPath = searchPath.Substring(relativePath.Length);
            }

            return Path.GetFullPath(basePath);
        }

        /// <summary>
        /// Returns true if the path contains any wildcard characters.
        /// </summary>
        internal static bool IsWildcardSearch(string filter)
        {
            return filter.IndexOf('*') != -1;
        }

        internal static bool IsDirectoryPath(string path)
        {
            return path != null && path.Length > 1 && path[path.Length - 1] == Path.DirectorySeparatorChar;
        }

        private static bool IsEmptyDirectory(string directory)
        {
            return !Directory.EnumerateFileSystemEntries(directory).Any();
        }
    }
}