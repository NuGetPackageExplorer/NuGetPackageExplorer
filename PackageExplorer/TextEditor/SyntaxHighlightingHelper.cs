using System;
using System.IO;
using System.Linq;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PackageExplorer
{
    internal static class SyntaxHighlightingHelper
    {
        private static bool _hasRegistered;
        private static readonly object _lock = new object();
        private static readonly string[] _nugetExtensions = new[] { ".nuspec", ".props", ".targets", ".xdt" };

        public static void RegisterHightingExtensions()
        {
            if (!_hasRegistered)
            {
                lock (_lock)
                {
                    if (!_hasRegistered)
                    {
                        _hasRegistered = true;

                        HighlightingManager.Instance.RegisterHighlighting(
                            "Plain Text",
                            new[] { ".txt" },
                            TextHighlightingDefinition.Instance);
                    }
                }
            }
        }

        public static IHighlightingDefinition GuessHighligtingDefinition(string name)
        {
            if (name.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase))
            {
                // don't try to highlight mini-fied JS and CSS file
                return TextHighlightingDefinition.Instance;
            }

            var extension = Path.GetExtension(name).ToUpperInvariant();
            if (_nugetExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                // treat these extension as xml
                extension = ".XML";
            }
            else if (extension == ".PP" || extension == ".TRANSFORM")
            {
                // if the extension is .pp or .transform, it is NuGet transform files.
                // in which case, we strip out this extension and examine the real extension instead

                name = Path.GetFileNameWithoutExtension(name);
                extension = Path.GetExtension(name).ToUpperInvariant();
            }

            return HighlightingManager.Instance.GetDefinitionByExtension(extension) ?? TextHighlightingDefinition.Instance;
        }
    }
}
