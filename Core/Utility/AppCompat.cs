using System;
using System.Runtime.InteropServices;

namespace NuGetPe
{
    public static class AppCompat
    {
        private static readonly Lazy<bool> isWindows10S;
        public static bool IsWindows10S => isWindows10S.Value;

        static AppCompat()
        {
            isWindows10S = new Lazy<bool>(GetIsWin10S);
        }

        private static bool GetIsWin10S()
        {
            GetProductInfo(
                Environment.OSVersion.Version.Major,
                Environment.OSVersion.Version.Minor,
                0,
                0,
                out var productNum);

            // https://msdn.microsoft.com/en-us/library/ms724358(v=vs.85).aspx

            /*
                PRODUCT_CLOUD                0x000000B2                Windows 10 S
                PRODUCT_CLOUDN               0x000000B3                Windows 10 S N
             */

            const int PRODUCT_CLOUD = 0x000000B2;
            const int PRODUCT_CLOUDN = 0x000000B3;

            return productNum == PRODUCT_CLOUD || productNum == PRODUCT_CLOUDN;
        }

        [DllImport("kernel32.dll", SetLastError = false)]
        private static extern bool GetProductInfo(
            int dwOSMajorVersion,
            int dwOSMinorVersion,
            int dwSpMajorVersion,
            int dwSpMinorVersion,
            out int pdwReturnedProductType);
    }
}
