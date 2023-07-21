using System;
using System.Linq;
using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]

namespace NuGetPe
{
    public static class AppCompat
    {
#pragma warning disable IDE1006 // Naming Styles
        private static readonly Lazy<bool> isWindows10S = new Lazy<bool>(GetIsWin10S);
#pragma warning restore IDE1006 // Naming Styles

        public static bool IsWindows10S => isWindows10S.Value;

        public static bool IsWasm => RuntimeInformation.OSArchitecture ==
#if NET5_0
            Architecture.Wasm;
#else
            (Architecture)4; // Architecture.Wasm definition is missing under NETSTANDARD2_1 & NETCOREAPP3_1
#endif

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsSupported(params RuntimeFeature[] features) => features.All(IsSupported);
        public static bool IsSupported(RuntimeFeature feature)
        {
            return feature switch
            {
                // Under Wasm (net7), the following is not supported
                // PrimarySignature.Load failed System.TypeInitializationException: TypeInitialization_Type, System.Security.Cryptography.Pkcs.SubjectIdentifier
                // dotnet.js:14--->System.TypeInitializationException: TypeInitialization_Type, System.Security.Cryptography.X509Certificates.X509Pal
                // dotnet.js:14--->System.PlatformNotSupportedException: SystemSecurityCryptographyX509Certificates_PlatformNotSupported
                // dotnet.js:14    at System.Security.Cryptography.X509Certificates.X509Pal.BuildSingleton() in runtime\src\libraries\System.Security.Cryptography\src\System\Security\Cryptography\X509Certificates\X509Pal.NotSupported.cs:line 10
                RuntimeFeature.Cryptography => !IsWasm,
                RuntimeFeature.NativeMethods => IsWindows,
                RuntimeFeature.DiaSymReader => IsWindows,

                _ => throw new ArgumentOutOfRangeException($"Unknown feature flag: {feature}")
            };
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

    public enum RuntimeFeature
    {
        /// <summary>
        /// affects: X509, Oid, Pkcs, SignedCms
        /// </summary>
        Cryptography,
        NativeMethods,
        DiaSymReader,
    }
}
