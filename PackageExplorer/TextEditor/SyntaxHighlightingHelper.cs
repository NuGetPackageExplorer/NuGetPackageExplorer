using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Highlighting;

namespace PackageExplorer
{
    internal static class SyntaxHighlightingHelper
    {
        private static bool _hasRegistered;
        private static readonly object _lock = new object();

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

                        var xmlHighlighter = HighlightingManager.Instance.GetDefinitionByExtension(".xml");
                        HighlightingManager.Instance.RegisterHighlighting(
                            "Xml", new[] { ".nuspec", ".props", ".targets" }, xmlHighlighter);
                    }
                }
            }
        }

        public static IHighlightingDefinition GuessHighligtingDefinition(string name)
        {
            string extension = Path.GetExtension(name).ToUpperInvariant();

            // if the extension is .pp or .transform, it is NuGet transform files.
            // in which case, we strip out this extension and examine the real extension instead
            if (extension == ".PP" || extension == ".TRANSFORM")
            {
                name = Path.GetFileNameWithoutExtension(name);
                extension = Path.GetExtension(name).ToUpperInvariant();
            }

            return HighlightingManager.Instance.GetDefinitionByExtension(extension) ?? TextHighlightingDefinition.Instance;
        }
    }
}