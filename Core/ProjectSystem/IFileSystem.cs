using System;
using System.Collections.Generic;
using System.IO;

namespace NuGetPe
{
    public interface IFileSystem
    {
        //ILogger Logger { get; set; }
        string Root { get; }
        void DeleteDirectory(string path, bool recursive);
        IEnumerable<string> GetFiles(string path);
        IEnumerable<string> GetFiles(string path, string filter);
        IEnumerable<string> GetDirectories(string path);
        string GetFullPath(string path);
        void DeleteFile(string path);
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void AddFile(string path, Stream stream);
        Stream OpenFile(string path);
        DateTimeOffset GetLastModified(string path);
        DateTimeOffset GetCreated(string path);
    }
}