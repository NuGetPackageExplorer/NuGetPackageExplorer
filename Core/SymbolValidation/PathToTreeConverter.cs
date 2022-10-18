using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;

using NuGetPe.AssemblyMetadata;

namespace NuGetPe
{
    internal static class PathToTreeConverter
    {
        public static IFolder Convert(List<NuGet.Packaging.IPackageFile> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            paths.Sort((p1, p2) => string.Compare(p1.Path, p2.Path, StringComparison.OrdinalIgnoreCase));

            var root = new Folder("", parent: null);

            var parsedPaths = paths.Select(p => Tuple.Create<NuGet.Packaging.IPackageFile, string[]>(p, p.Path.Split('\\'))).ToList();
            Parse(root, parsedPaths, 0, 0, parsedPaths.Count);

            return root;
        }

        private static void Parse(Folder root, List<Tuple<NuGet.Packaging.IPackageFile, string[]>> parsedPaths, int level, int start, int end)
        {
            var i = start;
            while (i < end)
            {
                var s = parsedPaths[i].Item2[level];

                if (parsedPaths[i].Item2.Length == level + 1)
                {
                    // it's a file
                    // Starting from nuget 2.0, they use a dummy file with the name "_._" to represent
                    // an empty folder. Therefore, we just ignore it.
                    if (!s.Equals(Constants.PackageEmptyFileName, StringComparison.OrdinalIgnoreCase))
                    {
                        root.Children.Add(new File(parsedPaths[i].Item1, s, root));
                    }
                    i++;
                }
                else
                {
                    // it's a folder
                    var j = i;
                    while (
                        j < end &&
                        level < parsedPaths[j].Item2.Length &&
                        parsedPaths[j].Item2[level].Equals(s, StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        j++;
                    }

                    var folder = new Folder(s, root);
                    root.Children.Add(folder);
                    Parse(folder, parsedPaths, level + 1, i, j);

                    i = j;
                }
            }
        }



        class File : IFile
        {
            private readonly NuGet.Packaging.IPackageFile _packageFile;

            public File(NuGet.Packaging.IPackageFile packageFile, string name, IFolder? parent)
            {
                _packageFile = packageFile ?? throw new ArgumentNullException(nameof(packageFile));
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Path = (parent?.Path ?? "") + (parent?.Parent == null ? "" : "\\") + name;
                Parent = parent;
                Extension = System.IO.Path.GetExtension(name);
            }

            public string Path { get; }

            public string Name { get; }

            public IFolder? Parent { get; }

            public IEnumerable<IFile> GetFiles()
            {
                yield return this;
            }

            public string? Extension { get; }

            public FrameworkName TargetFramework
            {
#pragma warning disable CS0618 // Type or member is obsolete
                get { return _packageFile.TargetFramework; }
#pragma warning restore CS0618 // Type or member is obsolete
            }

            public IEnumerable<IFile> GetAssociatedFiles()
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(Name);

                // Paths are normalized with "\" separators (see `entry.FullName.Replace('/', '\\')` in ZipPackageFile constructor and RecalculatePath in PackagePart)
                // and GetFileNameWithoutExtension is path separator sensitive, i.e. GetFileNameWithoutExtension(@"lib\net46\Microsoft.Data.SqlClient.pdb") returns
                // * `lib\net46\Microsoft.Data.SqlClient` on macOS and Linux
                // * `Microsoft.Data.SqlClient` on Windows
                // This is why we have to split on "\" and extract the last part
                static bool HasSameName(IPart packagePart, string name) =>
                    System.IO.Path.GetFileNameWithoutExtension(packagePart.Name).Equals(name, StringComparison.OrdinalIgnoreCase);

                return Parent!.GetFiles().Where(f => f.Path != Path && HasSameName(f, filename));
            }

            public AssemblyDebugData? DebugData { get; set; }

            public Stream GetStream() => _packageFile.GetStream();
        }

        class Folder : IFolder
        {
            public IFolder? Parent { get; }
            public SortedCollection<IPart> Children { get; }

            public Folder(string name, IFolder? parent)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
                Path = (parent?.Path ?? "") + (parent?.Parent == null ? "" : "\\") + name;
                Parent = parent;
                Children = new SortedCollection<IPart>(new PackagePartComparer());
            }

            public string Path { get; }

            public string Name { get; }

            public IEnumerable<IFile> GetFiles() => Children.SelectMany(e => e.GetFiles());

            public IPart? this[string name] => Children.SingleOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        class PackagePartComparer : Comparer<IPart>
        {
            public override int Compare(IPart? x, IPart? y)
            {
                return string.Compare(x?.Path, y?.Path, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
