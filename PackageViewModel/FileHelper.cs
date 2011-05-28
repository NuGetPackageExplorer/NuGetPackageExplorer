using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel {
    internal static class FileHelper {
        private static string[] _executableScriptsExtensions = new string[] {
            ".BAS", ".BAT", ".CHM", ".COM", ".EXE", ".HTA", ".INF", ".JS", ".LNK", ".MSI", 
            ".OCX", ".PPT", ".REG", ".SCT", ".SHS", ".SYS", ".URL", ".VB", ".VBS", ".WSH", ".WSF"
        };

        public static void OpenFileInShell(PackageFile file, IUIServices uiServices) {
            if (IsExecutableScript(file.Name)) {
                bool confirm = uiServices.Confirm(
                    String.Format(CultureInfo.CurrentCulture, Resources.OpenExecutableScriptWarning_Title, file.Name), 
                    Resources.OpenExecutableScriptWarning, 
                    isWarning: true);
                if (!confirm) {
                    return;
                }
            }

            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            string tempFileName = Path.Combine(Path.GetTempPath(), file.Name);
            using (Stream tempFileStream = File.Create(tempFileName)) {
                file.GetStream().CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName)) {
                Process.Start("explorer.exe", tempFileName);
            }
        }

        private static bool IsExecutableScript(string fileName) {
            string extension = Path.GetExtension(fileName).ToUpperInvariant();
            return Array.IndexOf(_executableScriptsExtensions, extension) > -1;
        }
        
        public static void OpenFileInShellWith(PackageFile file) {
            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            string tempFileName = Path.Combine(Path.GetTempPath(), file.Name);

            using (Stream tempFileStream = File.Create(tempFileName)) {
                file.GetStream().CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName)) {
                ProcessStartInfo info = new ProcessStartInfo("rundll32.exe") {
                    ErrorDialog = true,
                    UseShellExecute = false,
                    Arguments = "shell32.dll,OpenAs_RunDLL " + tempFileName
                };

                Process.Start(info);
            }
        }

        public static string GuessFolderNameFromFile(string file) {
            string extension = System.IO.Path.GetExtension(file).ToUpperInvariant();
            if (extension == ".DLL" || extension == ".PDB") {
                return "lib";
            }
            else if (extension == ".PS1" || extension == ".PSM1" || extension == ".PSD1") {
                return "tools";
            }
            else {
                return "content";
            }
        }
    }
}
