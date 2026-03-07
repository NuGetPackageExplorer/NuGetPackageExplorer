using System;
using System.IO;
using System.Linq;

namespace NuGetPe
{
    public static class PackagePathUtility
    {
        public static string NormalizeRelativePath(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var normalized = path.Contains('%', StringComparison.Ordinal)
                ? Uri.UnescapeDataString(path)
                : path;

            normalized = normalized.Replace('/', '\\');

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new InvalidDataException("Package paths must not be empty.");
            }

            if (normalized.StartsWith('\\') || HasDrivePrefix(normalized) || normalized.StartsWith(@"\\", StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Package path '{path}' must be relative.");
            }

            var segments = normalized.Split('\\');
            if (segments.Any(static segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".." || segment.Any(char.IsControl)))
            {
                throw new InvalidDataException($"Package path '{path}' contains an invalid segment.");
            }

            return string.Join("\\", segments);
        }

        public static string ResolvePathUnderRoot(string rootPath, string path)
        {
            ArgumentNullException.ThrowIfNull(rootPath);

            var fullRootPath = Path.GetFullPath(rootPath);
            var normalizedPath = NormalizeRelativePath(path);
            var resolvedPath = Path.GetFullPath(Path.Combine(fullRootPath, normalizedPath));
            var rootPrefix = fullRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!resolvedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Package path '{path}' resolves outside the selected root.");
            }

            return resolvedPath;
        }

        public static string NormalizePathSegment(string segment)
        {
            var normalizedSegment = NormalizeRelativePath(segment);
            if (normalizedSegment.Contains('\\', StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Package path segment '{segment}' must not contain directory separators.");
            }

            return normalizedSegment;
        }

        private static bool HasDrivePrefix(string path)
        {
            return path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':';
        }
    }
}
