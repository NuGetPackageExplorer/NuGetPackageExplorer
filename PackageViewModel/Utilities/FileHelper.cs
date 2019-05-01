using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using NuGetPackageExplorer.Types;
using NuGetPe;

namespace PackageExplorerViewModel
{
    internal static class FileHelper
    {
        private static readonly string[] ExecutableScriptsExtensions = new[]
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
                                                                    ".VSIX", ".NUPKG", ".SNUPKG", ".SNK", ".PFX", ".ICO"
                                                                };

        public static bool IsBinaryFile(string path)
        {
            // TODO: check for content type of the file here
            var extension = Path.GetExtension(path).ToUpper(CultureInfo.InvariantCulture);
            return !string.IsNullOrEmpty(extension) && BinaryFileExtensions.Any(p => p.Equals(extension));
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
            using (var packageStream = file.GetStream())
            {
                packageStream.CopyTo(tempFileStream);
            }

            if (File.Exists(tempFileName))
            {
                try
                {
                    Process.Start("explorer.exe", tempFileName);
                }
                catch // Possible Win32 exception, nothing we can do
                {
                }
            }
        }

        private static bool IsExecutableScript(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToUpperInvariant();
            return Array.IndexOf(ExecutableScriptsExtensions, extension) > -1;
        }

        public static void OpenFileInShellWith(PackageFile file)
        {
            DiagnosticsClient.TrackEvent("FileHelper_OpenFileInShellWith");

            // copy to temporary file
            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            var tempFileName = Path.Combine(GetTempFilePath(), file.Name);

            using (Stream tempFileStream = File.Create(tempFileName))
            using (var packageStream = file.GetStream())
            {
                packageStream.CopyTo(tempFileStream);
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

                try
                {
                    Process.Start(info);
                }
                catch // Possible Win32 exception, nothing we can do
                {
                }
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


        [Flags]
        private enum SHGFI
        {
            /// <summary>
            /// Get icon
            /// </summary>
            Icon = 0x000000100,

            /// <summary>
            /// get display name
            /// </summary>
            DisplayName = 0x000000200,

            /// <summary>
            /// Get type name
            /// </summary>
            TypeName = 0x000000400,

            /// <summary>
            /// Get attributes
            /// </summary>
            Attributes = 0x000000800,

            /// <summary>
            /// Get icon location
            /// </summary>
            IconLocatin = 0x000001000,

            /// <summary>
            /// Return exe type
            /// </summary>
            ExeType = 0x000002000,

            /// <summary>
            /// Get system icon index
            /// </summary>
            SysIconIndex = 0x000004000,

            /// <summary>
            /// Put a link overlay on icon
            /// </summary>
            LinkOverlay = 0x000008000,

            /// <summary>
            /// Show icon in selected state
            /// </summary>
            Selected = 0x000010000,

            /// <summary>
            /// Get only specified attributes
            /// </summary>
            Attr_Specified = 0x000020000,

            /// <summary>
            /// Get large icon
            /// </summary>
            LargeIcon = 0x000000000,

            /// <summary>
            /// Get small icon
            /// </summary>
            SmallIcon = 0x000000001,

            /// <summary>
            /// Get open icon
            /// </summary>
            OpenIcon = 0x000000002,

            /// <summary>
            /// Get shell size icon
            /// </summary>
            ShellIconize = 0x000000004,

            /// <summary>
            /// pszPath is a pidl
            /// </summary>
            PIDL = 0x000000008,

            /// <summary>
            /// Use passed dwFileAttribute
            /// </summary>
            UseFileAttributes = 0x000000010,

            /// <summary>
            /// Apply the appropriate overlays
            /// </summary>
            AddOverlays = 0x000000020,

            /// <summary>
            /// Get the index of the overlay in the upper 8 bits of the iIcon
            /// </summary>
            OverlayIndex = 0x000000040,

            /// <summary>
            /// The handle that identifies a directory.
            /// </summary>
            FILE_ATTRIBUTE_DIRECTORY = 0x10,
        }

        /// <summary>
        /// Maximal Length of unmanaged Windows-Path-strings
        /// </summary>
        private const int MAX_PATH = 260;

        /// <summary>
        /// Maximal Length of unmanaged Typename
        /// </summary>
        private const int MAX_TYPE = 80;

        private const int FILE_ATTRIBUTE_NORMAL = 0x80;


        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, out SHFILEINFO psfi, uint cbfileInfo, SHGFI uFlags);

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);

#pragma warning disable IDE1006 // Naming Styles
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;

            public int iIcon;

            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
            public string szTypeName;
        }
#pragma warning restore IDE1006 // Naming Styles
        public static Icon ExtractAssociatedIcon(string fileName)
        {
            var info = new SHFILEINFO();
            var infoSize = (uint)Marshal.SizeOf(info);

            SHGetFileInfo(
                fileName,
                FILE_ATTRIBUTE_NORMAL,
                out info,
                infoSize,
                SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes);

            var icon = (Icon)Icon.FromHandle(info.hIcon).Clone();
            DestroyIcon(info.hIcon);
            return icon;
        }

    }
}
