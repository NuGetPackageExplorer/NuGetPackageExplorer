using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PackageExplorer
{
    internal static class WindowPlacementHelper
    {
        public static void LoadWindowPlacementFromSettings(this Window window, string setting)
        {
            if (string.IsNullOrEmpty(setting))
            {
                return;
            }

            var wp = WindowPlacement.Parse(setting);
            wp.length = Marshal.SizeOf(typeof(WindowPlacement));
            wp.flags = 0;
            wp.showCmd = (wp.showCmd == NativeMethods.SW_SHOWMINIMIZED ? NativeMethods.SW_SHOWNORMAL : wp.showCmd);

            var hwnd = new WindowInteropHelper(window).Handle;
            NativeMethods.SetWindowPlacement(hwnd, ref wp);
        }

        public static string SaveWindowPlacementToSettings(this Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            NativeMethods.GetWindowPlacement(hwnd, out var wp);

            return wp.ToString();
        }
    }
}