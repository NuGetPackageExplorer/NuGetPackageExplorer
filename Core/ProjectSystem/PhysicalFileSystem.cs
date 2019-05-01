using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGetPe
{
    public class PhysicalFileSystem : IFileSystem
    {
        public PhysicalFileSystem(string root)
        {
            if (string.IsNullOrEmpty(root))
            {
                throw new ArgumentException("Argument cannot be null or empty.", "root");
            }
            Root = root;
        }

        #region IFileSystem Members

        public string Root { get; }

        public virtual string GetFullPath(string path)
        {
            return Path.Combine(Root, path);
        }

        public virtual void AddFile(string path, Stream stream)
        {
            EnsureDirectory(Path.GetDirectoryName(path));

            using Stream outputStream = File.Create(GetFullPath(path));
            stream.CopyTo(outputStream);
        }

        public virtual void DeleteFile(string path)
        {
            if (!FileExists(path))
            {
                return;
            }

            try
            {
                path = GetFullPath(path);
                File.Delete(path);
            }
            catch (FileNotFoundException)
            {
            }
        }

        public virtual void DeleteDirectory(string path, bool recursive)
        {
            if (!DirectoryExists(path))
            {
                return;
            }

            try
            {
                path = GetFullPath(path);
                Directory.Delete(path, recursive);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        public virtual IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, "*.*");
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter)
        {
            path = EnsureTrailingSlash(GetFullPath(path));
            try
            {
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateFiles(path, filter)
                    .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return Enumerable.Empty<string>();
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            try
            {
                path = EnsureTrailingSlash(GetFullPath(path));
                if (!Directory.Exists(path))
                {
                    return Enumerable.Empty<string>();
                }
                return Directory.EnumerateDirectories(path)
                    .Select(MakeRelativePath);
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }

            return Enumerable.Empty<string>();
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            if (DirectoryExists(path))
            {
                return new DirectoryInfo(GetFullPath(path)).LastWriteTimeUtc;
            }
            return new FileInfo(GetFullPath(path)).LastWriteTimeUtc;
        }

        public DateTimeOffset GetCreated(string path)
        {
            if (DirectoryExists(path))
            {
                return Directory.GetCreationTimeUtc(GetFullPath(path));
            }
            return File.GetCreationTimeUtc(GetFullPath(path));
        }

        public virtual bool FileExists(string path)
        {
            path = GetFullPath(path);
            return File.Exists(path);
        }

        public virtual bool DirectoryExists(string path)
        {
            path = GetFullPath(path);
            return Directory.Exists(path);
        }

        public virtual Stream OpenFile(string path)
        {
            path = GetFullPath(path);
            return File.OpenRead(path);
        }

        #endregion

        public virtual void DeleteDirectory(string path)
        {
            DeleteDirectory(path, recursive: false);
        }

        protected string MakeRelativePath(string fullPath)
        {
            return fullPath.Substring(Root.Length).TrimStart(Path.DirectorySeparatorChar);
        }

        protected virtual void EnsureDirectory(string path)
        {
            path = GetFullPath(path);
            Directory.CreateDirectory(path);
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (!path.EndsWith("\\", StringComparison.Ordinal))
            {
                path += "\\";
            }
            return path;
        }
    }
}
