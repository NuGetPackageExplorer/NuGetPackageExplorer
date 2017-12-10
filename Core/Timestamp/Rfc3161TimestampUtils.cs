// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Security.Cryptography.Pkcs;


using System.Text;

namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Internally used by Rfc3161TimestampProvider.
    /// This class should be removed once we can reference it throught the .NET Core framework.
    /// </summary>
    internal static class Rfc3161TimestampUtils
    {
        internal static bool IsLegalOid(string algorithmIdentifier)
        {
            // All OIDs require at least a.b
            if (algorithmIdentifier == null || algorithmIdentifier.Length < 3)
                return false;

            bool allowPeriod = false;
            bool startsWith0 = false;

            foreach (char c in algorithmIdentifier)
            {
                if (c == '0')
                {
                    if (startsWith0)
                    {
                        return false;
                    }

                    if (allowPeriod)
                    {
                        continue;
                    }

                    allowPeriod = true;
                    startsWith0 = true;
                    continue;
                }

                if (c > '0' && c <= '9')
                {
                    if (startsWith0)
                    {
                        return false;
                    }

                    allowPeriod = true;
                    continue;
                }

                if (allowPeriod && c == '.')
                {
                    // Cannot have two periods in a row.
                    allowPeriod = false;
                    startsWith0 = false;
                    continue;
                }

                return false;
            }

            // Cannot end with a period.
            return allowPeriod;
        }
        
        public static byte[] GetSignature(this SignerInfo signerInfo)
        {
            var field = typeof(SignerInfo).GetField("m_pbCmsgSignerInfo", BindingFlags.Instance | BindingFlags.NonPublic);
            SafeHandle pbCmsgSignerInfo = (SafeHandle)field.GetValue(signerInfo);

            byte[] ret;
            unsafe
            {
                CMSG_SIGNER_INFO* ptr = (CMSG_SIGNER_INFO*)pbCmsgSignerInfo.DangerousGetHandle();
                ret = new byte[ptr->EncryptedHash.cbData];
                Marshal.Copy(ptr->EncryptedHash.pbData, ret, 0, ret.Length);
                return ret;
            }
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CMSG_SIGNER_INFO
        {
            internal uint dwVersion;
            internal Rfc3161TimestampWin32.CRYPTOAPI_BLOB Issuer;
            internal Rfc3161TimestampWin32.CRYPTOAPI_BLOB SerialNumber;
            internal Rfc3161TimestampWin32.CRYPT_ALGORITHM_IDENTIFIER HashAlgorithm;
            internal Rfc3161TimestampWin32.CRYPT_ALGORITHM_IDENTIFIER HashEncryptionAlgorithm;
            internal Rfc3161TimestampWin32.CRYPTOAPI_BLOB EncryptedHash;
            internal CRYPT_ATTRIBUTES AuthAttrs;
            internal CRYPT_ATTRIBUTES UnauthAttrs;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPT_ATTRIBUTES
        {
            internal uint cAttr;
            internal IntPtr rgAttr;     // PCRYPT_ATTRIBUTE
        }

        internal static string ByteArrayToHex(this byte[] bytes)
        {
            if (bytes == null)
                return string.Empty;

            StringBuilder builder = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("X2"));
            }

            return builder.ToString();
        }

        internal static byte[] HexToByteArray(this string hexString)
        {
            byte[] bytes = new byte[hexString.Length / 2];

            for (int i = 0; i < hexString.Length; i += 2)
            {
                string s = hexString.Substring(i, 2);
                bytes[i / 2] = byte.Parse(s, NumberStyles.HexNumber, null);
            }

            return bytes;
        }
    }
}
