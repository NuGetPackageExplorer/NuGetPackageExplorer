using System;
using System.Runtime.InteropServices;

namespace PackageExplorer
{
    internal static class NativeMethods
    {
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const uint WM_SETICON = 0x0080;

        public static bool IsWindows8OrLater
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                       Environment.OSVersion.Version >= new Version(6, 2, 9200);
            }
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, out WindowPlacement lpwndpl);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height,
                                               uint flags);

        [DllImport("user32.dll", PreserveSig = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, long wParam, IntPtr lParam);

        [method: DllImport("ntdsapi.dll", EntryPoint = "DsGetRdnW", ExactSpelling = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Error)]
        public static extern int DsGetRdnW(
            [param: In, Out, MarshalAs(UnmanagedType.SysInt)] ref IntPtr ppDN,
            [param: In, Out, MarshalAs(UnmanagedType.U4)] ref uint pcDN,
            [param: Out, MarshalAs(UnmanagedType.SysInt)] out IntPtr ppKey,
            [param: Out, MarshalAs(UnmanagedType.U4)] out uint pcKey,
            [param: Out, MarshalAs(UnmanagedType.SysInt)] out IntPtr ppVal,
            [param: Out, MarshalAs(UnmanagedType.U4)] out uint pcVal
        );
    }
}
