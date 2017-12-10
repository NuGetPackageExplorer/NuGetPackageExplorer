// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Internally used by Rfc3161TimestampProvider.
    /// This class should be removed once we can reference it throught the .NET Core framework.
    /// </summary>
    internal static class Rfc3161TimestampWin32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPTOAPI_BLOB
        {
            internal uint cbData;
            internal IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_TIMESTAMP_PARA
        {
            public IntPtr pszTSAPolicyId;
            public bool fRequestCerts;
            public CRYPTOAPI_BLOB Nonce;
            public int cExtension;
            public IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_TIMESTAMP_CONTEXT
        {
            public int cbEncoded;
            public IntPtr pbEncoded;
            public IntPtr pTimeStamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_ALGORITHM_IDENTIFIER
        {
            public IntPtr pszOid;
            public CRYPTOAPI_BLOB Parameters;
        }

        internal struct CRYPT_TIMESTAMP_ACCURACY
        {
            public int dwSeconds;
            public int dwMillis;
            public int dwMicros;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_TIMESTAMP_INFO
        {
            public int dwVersion;
            public IntPtr pszTSAPolicyId;
            public CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;
            public CRYPTOAPI_BLOB HashedMessage;
            public CRYPTOAPI_BLOB SerialNumber;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftTime;
            public IntPtr pvAccuracy;
            [MarshalAs(UnmanagedType.Bool)] public bool fOrdering;
            public CRYPTOAPI_BLOB Nonce;
            public CRYPTOAPI_BLOB Tsa;
            public int cExtension;
            public IntPtr rgExtension;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_TIMESTAMP_REQUEST
        {
            public int dwVersion;
            public CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;
            public CRYPTOAPI_BLOB HashedMessage;
            public IntPtr pszTSAPolicyId;
            public CRYPTOAPI_BLOB Nonce;
            public bool fCertReq;
            public int cExtension;
            public IntPtr rgExtension;
        }

        [Flags]
        internal enum CryptRetrieveTimeStampFlags
        {
            TIMESTAMP_DONT_HASH_DATA = 1,
            TIMESTAMP_VERIFY_CONTEXT_SIGNATURE = 0x20,
            TIMESTAMP_NO_AUTH_RETRIEVAL = 0x00020000,
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern bool CryptRetrieveTimeStamp(
            [MarshalAs(UnmanagedType.LPWStr)] string wszUrl,
            CryptRetrieveTimeStampFlags dwRetrievalFlags,
            int dwTimeout,
            [MarshalAs(UnmanagedType.LPStr)] string pszHashId,
            ref CRYPT_TIMESTAMP_PARA pPara,
            [In] byte[] pbData,
            int cbData,
            ref IntPtr ppTsContext,
            ref IntPtr ppTsSigner,
            ref IntPtr phStore);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern bool CryptVerifyTimeStampSignature(
            [In] byte[] pbTSContentInfo,
            int cbTSContentInfo,
            [In] byte[] pbData,
            int cbData,
            IntPtr hAdditionalStore,
            ref IntPtr ppTsContext,
            ref IntPtr ppTsSigner,
            ref IntPtr phStore);

        [Flags]
        internal enum CryptEncodingTypes
        {
            X509_ASN_ENCODING = 0x1,
            PKCS_7_ASN_ENCODING = 0x10000,
        }

        [Flags]
        internal enum CryptEncodeObjectFlags
        {
            None = 0,
            CRYPT_ENCODE_ALLOC_FLAG = 0x8000,
        }

        [Flags]
        internal enum CryptDecodeObjectFlags
        {
            None = 0,
            CRYPT_DECODE_NOCOPY_FLAG = 0x1,
            CRYPT_DECODE_NO_SIGNATURE_BYTE_REVERSAL_FLAG = 0x8,
            CRYPT_DECODE_ALLOC_FLAG = 0x8000,
        }

        internal static readonly IntPtr TIMESTAMP_REQUEST = new IntPtr(78);
        internal static readonly IntPtr TIMESTAMP_INFO = new IntPtr(80);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", SetLastError = true, BestFitMapping = false)]
        public static extern bool CryptEncodeObjectEx(
            CryptEncodingTypes dwCertEncodingType,
            IntPtr lpszStructType,
            IntPtr pvStructInfo,
            CryptEncodeObjectFlags dwFlags,
            IntPtr pEncodePara,
            IntPtr pvEncoded,
            ref uint pcbEncoded);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", SetLastError = true, BestFitMapping = false)]
        public static extern bool CryptDecodeObjectEx(
            CryptEncodingTypes dwCertEncodingType,
            IntPtr lpszStructType,
            IntPtr pbEncoded,
            int cbEncoded,
            CryptDecodeObjectFlags dwFlags,
            IntPtr pDecodePara,
            IntPtr pvStructInfo,
            ref int pcbStructInfo);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        internal static extern bool CertCloseStore(IntPtr pCertContext, int dwFlags);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("crypt32.dll", CallingConvention = CallingConvention.Winapi)]
        internal static extern void CryptMemFree(IntPtr pv);

        [DllImport("kernel32.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern IntPtr LocalFree(IntPtr handle);
    }
}
