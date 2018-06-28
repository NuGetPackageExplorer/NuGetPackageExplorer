using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal static class FileHelper
    {
        private static readonly string[] _executableScriptsExtensions = new[]
                                                                        {
                                                                            ".BAS", ".BAT", ".CHM", ".COM", ".EXE",
                                                                            ".HTA", ".INF", ".JS", ".LNK", ".MSI",
                                                                            ".OCX", ".PPT", ".REG", ".SCT", ".SHS",
                                                                            ".SYS", ".URL", ".VB", ".VBS", ".WSH",
                                                                            ".WSF"
                                                                        };

        private static readonly string[] BinaryFileExtensions = new[]
                                                                {
                                                                    ".DLL", ".EXE", ".WINMD", ".CHM", ".PDF",
                                                                    ".DOCX", ".DOC", ".JPG", ".PNG", ".GIF",
                                                                    ".RTF", ".PDB", ".ZIP", ".RAR", ".XAP",
                                                                    ".VSIX", ".NUPKG", ".SNK", ".PFX", ".ICO"
                                                                };

        public static bool IsBinaryFile(string path)
        {
            // TODO: check for content type of the file here
            var extension = Path.GetExtension(path).ToUpper(CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(extension) || BinaryFileExtensions.Any(p => p.Equals(extension));
        }

        public static void OpenFileInShell(PackageFile file, IUIServices uiServices)
        {
            if (IsExecutableScript(file.Name))
            {
                var confirm = uiServices.Confirm(
                    string.Format(CultureInfo.CurrentCulture, Resources.OpenExecutableScriptWarning_Title, file.Name),
                    Resources.OpenExecutableScriptWarning,
                    isWarning: true);
                if (!confirm)
                {
                    return;
                }
            }

            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            var tempFileName = Path.Combine(GetTempFilePath(), file.Name);
            using (Stream tempFileStream = File.Create(tempFileName))
            {
                file.GetStream().CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName))
            {
                Process.Start("explorer.exe", tempFileName);
            }
        }

        private static bool IsExecutableScript(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToUpperInvariant();
            return Array.IndexOf(_executableScriptsExtensions, extension) > -1;
        }

        public static void OpenFileInShellWith(PackageFile file)
        {
            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            var tempFileName = Path.Combine(GetTempFilePath(), file.Name);

            using (Stream tempFileStream = File.Create(tempFileName))
            {
                file.GetStream().CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName))
            {
                var info = new ProcessStartInfo("rundll32.exe")
                {
                    ErrorDialog = true,
                    UseShellExecute = false,
                    Arguments =
                                   "shell32.dll,OpenAs_RunDLL " + tempFileName
                };

                Process.Start(info);
            }
        }

        public static string GuessFolderNameFromFile(string file)
        {
            var extension = Path.GetExtension(file).ToUpperInvariant();
            if (extension == ".DLL" || extension == ".PDB")
            {
                return "lib";
            }
            else if (extension == ".PS1" || extension == ".PSM1" || extension == ".PSD1")
            {
                return "tools";
            }
            else
            {
                return "content";
            }
        }

        public static string GetTempFilePath()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            return tempPath;
        }

        public static string CreateTempFile(string fileName, string content = "")
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Argument is null or empty", "fileName");
            }

            var filePath = Path.Combine(GetTempFilePath(), fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        public static string CreateTempFile(string fileName, Stream content)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Argument is null or empty", "fileName");
            }

            var filePath = Path.Combine(GetTempFilePath(), fileName);
            using (Stream targetStream = File.Create(filePath))
            {
                content.CopyTo(targetStream);
            }
            return filePath;
        }

        public static bool IsAssembly(string path)
        {
            return path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".winmd", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
        }
    }
}