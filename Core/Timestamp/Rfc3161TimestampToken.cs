// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using System.Security.Cryptography.Pkcs;


using System.Security.Cryptography.X509Certificates;

namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Class representing a Rfc3161TimestampToken.
    /// This class should be removed once we can reference it throught the .NET Core framework.
    /// </summary>
    internal sealed class Rfc3161TimestampToken
    {



        private readonly byte[] _encoded;

        public Rfc3161TimestampTokenInfo TokenInfo { get; }
        public X509Certificate2 SignerCertificate { get; }
        public X509Certificate2Collection AdditionalCerts { get; }

        internal Rfc3161TimestampToken(
            Rfc3161TimestampTokenInfo tstInfo,
            X509Certificate2 signerCertificate,
            X509Certificate2Collection additionalCerts,
            byte[] encoded)
        {
            Debug.Assert(tstInfo != null);
            Debug.Assert(signerCertificate != null);
            Debug.Assert(additionalCerts != null);

            TokenInfo = tstInfo;
            SignerCertificate = signerCertificate;
            AdditionalCerts = additionalCerts;
            _encoded = encoded;
        }

        public byte[] GetEncodedValue() => (byte[])_encoded.Clone();

        public SignedCms AsSignedCms()
        {
            SignedCms signedCms = new SignedCms();
            signedCms.Decode(_encoded);

            return signedCms;
        }

        public static Rfc3161TimestampToken LoadOnly(byte[] encodedToken)
        {
            if (encodedToken == null)
                throw new ArgumentNullException(nameof(encodedToken));

            return CryptVerifyTimeStampSignature(encodedToken, null);
        }

        public static Rfc3161TimestampToken LoadAndVerifyData(byte[] encodedToken, byte[] data)
        {
            if (encodedToken == null)
                throw new ArgumentNullException(nameof(encodedToken));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return CryptVerifyTimeStampSignature(encodedToken, data);
        }

        public static Rfc3161TimestampToken LoadAndVerifyHash(byte[] encodedToken, byte[] hash)
        {
            if (encodedToken == null)
                throw new ArgumentNullException(nameof(encodedToken));
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));

            Rfc3161TimestampToken token = CryptVerifyTimeStampSignature(encodedToken, null);

            if (!token.TokenInfo.HasMessageHash(hash))
            {
                const int NTE_BAD_HASH = unchecked((int)0x80090002);

                token.SignerCertificate?.Dispose();

                foreach (var cert in token.AdditionalCerts)
                {
                    cert?.Dispose();
                }

                throw new CryptographicException(NTE_BAD_HASH);
            }

            return token;
        }

        private static Rfc3161TimestampToken CryptVerifyTimeStampSignature(byte[] encodedToken, byte[] data)
        {
            IntPtr pTsContext = IntPtr.Zero;
            IntPtr pTsSigner = IntPtr.Zero;
            IntPtr hStore = IntPtr.Zero;

            try
            {
                if (!Rfc3161TimestampWin32.CryptVerifyTimeStampSignature(
                    encodedToken,
                    encodedToken.Length,
                    data,
                    data?.Length ?? 0,
                    IntPtr.Zero,
                    ref pTsContext,
                    ref pTsSigner,
                    ref hStore))
                {
                    throw new CryptographicException(Marshal.GetLastWin32Error());
                }

                Rfc3161TimestampTokenInfo tstInfo = new Rfc3161TimestampTokenInfo(pTsContext);
                X509Certificate2 signerCert = new X509Certificate2(pTsSigner);

                using (X509Store extraCerts = new X509Store(hStore))
                {
                    X509Certificate2Collection additionalCertsColl = new X509Certificate2Collection();

                    foreach (var cert in extraCerts.Certificates)
                    {
                        if (!signerCert.Equals(cert))
                        {
                            additionalCertsColl.Add(cert);
                        }
                    }

                    return new Rfc3161TimestampToken(
                        tstInfo,
                        signerCert,
                        additionalCertsColl,
                        (byte[])encodedToken.Clone());
                }
            }
            finally
            {
                if (pTsContext != IntPtr.Zero)
                    Rfc3161TimestampWin32.CryptMemFree(pTsContext);

                if (pTsSigner != IntPtr.Zero)
                    Rfc3161TimestampWin32.CertFreeCertificateContext(pTsSigner);

                if (hStore != IntPtr.Zero)
                    Rfc3161TimestampWin32.CertCloseStore(hStore, 0);
            }
        }
    }
}
