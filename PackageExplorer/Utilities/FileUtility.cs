using System;
using System.IO;
using NuGet;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PackageExplorer {
    internal static class FileUtility {
        public static bool IsSupportedFile(string filepath) {
            if (String.IsNullOrEmpty(filepath)) {
                return false;
            }

            string extension = Path.GetExtension(filepath);
            return extension.Equals(Constants.ManifestExtension, StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(Constants.PackageExtension, StringComparison.OrdinalIgnoreCase);
        }

        public static IHighlightingDefinition DeduceHighligtingDefinition(string name) {
            string extension = Path.GetExtension(name).ToUpperInvariant();

            // if the extension is .pp or .transform, it is NuGet transform files.
            // in which case, we strip out this extension and examine the real extension instead
            if (extension == ".PP" || extension == ".TRANSFORM") {
                name = Path.GetFileNameWithoutExtension(name);
                extension = Path.GetExtension(name).ToUpperInvariant();
            }

            return HighlightingManager.Instance.GetDefinitionByExtension(extension);
        }
    }
}
