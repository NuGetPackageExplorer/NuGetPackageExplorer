using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;

namespace PackageExplorer
{
    public class StandardDialog : Window
    {
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            const int GWL_STYLE = -16;
            const int GWL_EXSTYLE = -20;
            const int WS_EX_DLGMODALFRAME = 0x0001;

            const int SWP_NOSIZE = 0x0001;
            const int SWP_NOMOVE = 0x0002;
            const int SWP_NOZORDER = 0x0004;
            const int SWP_FRAMECHANGED = 0x0020;


            var hwnd = new WindowInteropHelper(this).Handle;

            var value = NativeMethods.GetWindowLong(hwnd, GWL_STYLE);
            NativeMethods.SetWindowLong(hwnd, GWL_STYLE, value & -131073 & -65537);

            var extendedStyle = NativeMethods.GetWindowLong(hwnd, GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);

            // Update the window's non-client area to reflect the changes
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                                       SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);

            if (!Debugger.IsAttached)
            {
                NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, 0, (IntPtr)0);
                NativeMethods.SendMessage(hwnd, NativeMethods.WM_SETICON, 1, (IntPtr)0);
            }
        }
    }
}