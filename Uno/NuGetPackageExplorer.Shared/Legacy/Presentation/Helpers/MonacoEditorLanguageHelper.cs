using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Uno.Extensions;
using Uno.Logging;

namespace NupkgExplorer.Presentation.Helpers
{
    public static class MonacoEditorLanguageHelper
    {
        private static readonly string[] NugetExtensions = new[] { ".nuspec", ".props", ".targets", ".xdt" };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "...")]
        public static string? MapFileNameToLanguage(string filename)
        {
            return Path.GetExtension(filename)?.ToLowerInvariant() switch
            {
                ".txt" => default,
                ".md" => "markdown",
                ".xml" => "xml",
                var x when NugetExtensions.Contains(x) => "xml",

                _ => default,
            };
        }
    }
}
