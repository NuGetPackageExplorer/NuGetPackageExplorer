using System;
using System.Collections.Generic;
using System.Linq;
using NuGet;

namespace PackageExplorerViewModel {
    internal static class PathToTreeConverter {

        public static PackageFolder Convert(List<IPackageFile> paths, PackageViewModel viewModel) {
            if (paths == null) {
                throw new ArgumentNullException("paths");
            }

            paths.Sort((p1, p2) => String.Compare(p1.Path, p2.Path, StringComparison.OrdinalIgnoreCase));

            PackageFolder root = new PackageFolder("", viewModel);

            List<Tuple<IPackageFile, string[]>> parsedPaths = paths.Select(p => Tuple.Create(p, p.Path.Split('\\'))).ToList();
            Parse(root, parsedPaths, 0, 0, parsedPaths.Count);

            return root;
        }

        private static void Parse(PackageFolder root, List<Tuple<IPackageFile, string[]>> parsedPaths, int level, int start, int end) {
            int i = start;
            while (i < end) {
                string s = parsedPaths[i].Item2[level];

                if (parsedPaths[i].Item2.Length == level + 1) {
                    // it's a file
                    root.Children.Add(new PackageFile(parsedPaths[i].Item1, s, root));
                    i++;
                }
                else {
                    // it's a folder
                    int j = i;
                    while (
                        j < end &&
                        level < parsedPaths[j].Item2.Length &&
                        parsedPaths[j].Item2[level].Equals(s, StringComparison.OrdinalIgnoreCase)
                    ) {
                        j++;
                    }

                    PackageFolder folder = new PackageFolder(s, root);
                    root.Children.Add(folder);
                    Parse(folder, parsedPaths, level + 1, i, j);

                    i = j;
                }
            }
        }
    }
}