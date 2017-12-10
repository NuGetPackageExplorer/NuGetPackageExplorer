// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;


namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Represents an RFC3161 TSTInfo.
    /// This class should be removed once we can reference it throught the .NET Core framework.
    /// </summary>
    internal sealed class Rfc3161TimestampTokenInfo : AsnEncodedData
    {

        public const string TimestampTokenInfoId = "1.2.840.113549.1.9.16.1.4";

        private TstInfo _decoded;

        private TstInfo Decoded
        {
            get
            {
                if (_decoded == null)
                    _decoded = Decode(RawData);

                return _decoded;
            }
        }

        private class TstInfo
        {
            public int Version { get; set; }
            public string PolicyId { get; set; }
            public Oid HashAlgorithmId { get; set; }
            public byte[] HashedMessage { get; set; }
            public byte[] SerialNumber { get; set; }
            public DateTimeOffset Timestamp { get; set; }
            public long? AccuracyInMicroseconds { get; set; }
            public bool IsOrdering { get; set; }
            public byte[] Nonce { get; set; }
            public byte[] TsaName { get; set; }
            public X509ExtensionCollection Extensions { get; set; }
        }

        public Rfc3161TimestampTokenInfo(byte[] timestampTokenInfo)
            : base(TimestampTokenInfoId, timestampTokenInfo)
        {
        }

        public Rfc3161TimestampTokenInfo(
            string policyId,
            Oid hashAlgorithmId,
            byte[] messageHash,
            byte[] serialNumber,
            DateTimeOffset timestamp,
            long? accuracyInMicroseconds = null,
            bool isOrdering = false,
            byte[] nonce = null,
            byte[] tsaName = null,
            X509ExtensionCollection extensions = null)
            : base(TimestampTokenInfoId, new byte[] { })
        {
            if (policyId == null)
                throw new ArgumentNullException(nameof(policyId));
            if (!Rfc3161TimestampUtils.IsLegalOid(policyId))
                throw new ArgumentException("Policy identifier does not represent a legal value", nameof(hashAlgorithmId));
            if (hashAlgorithmId == null)
                throw new ArgumentNullException(nameof(hashAlgorithmId));
            if (!Rfc3161TimestampUtils.IsLegalOid(hashAlgorithmId.Value))
                throw new ArgumentException("Hash algorithm does not represent a legal value", nameof(hashAlgorithmId));
            if (messageHash == null)
                throw new ArgumentNullException(nameof(messageHash));
            if (messageHash.Length == 0)
                throw new ArgumentException("Non-empty array is required", nameof(messageHash));
            if (serialNumber == null)
                throw new ArgumentNullException(nameof(serialNumber));
            if (serialNumber.Length == 0)
                throw new ArgumentException("Non-empty array is required", nameof(serialNumber));

            long accuracy = accuracyInMicroseconds.GetValueOrDefault();
            if (accuracy < 0 || accuracy > 4294967295000000)
                throw new ArgumentOutOfRangeException(nameof(accuracyInMicroseconds));

            TstInfo tstInfo = new TstInfo
            {
                Version = 1,
                PolicyId = policyId,
                HashAlgorithmId = new Oid(hashAlgorithmId.Value, hashAlgorithmId.FriendlyName),
                HashedMessage = (byte[])messageHash.Clone(),
                SerialNumber = (byte[])serialNumber.Clone(),
                Timestamp = timestamp.ToUniversalTime(),
                AccuracyInMicroseconds = accuracyInMicroseconds,
                IsOrdering = isOrdering,
                Nonce = (byte[])nonce?.Clone(),
                TsaName = (byte[])tsaName?.Clone(),
                Extensions = ShallowCopy(extensions, preserveNull: true),
            };

            RawData = Encode(tstInfo);
            _decoded = tstInfo;
        }

        internal Rfc3161TimestampTokenInfo(IntPtr pTsContext)
        {
            var context = (Rfc3161TimestampWin32.CRYPT_TIMESTAMP_CONTEXT)Marshal.PtrToStructure(pTsContext, typeof(Rfc3161TimestampWin32.CRYPT_TIMESTAMP_CONTEXT));
            byte[] encoded = new byte[context.cbEncoded];
            Marshal.Copy(context.pbEncoded, encoded, 0, context.cbEncoded);

            RawData = encoded;
            _decoded = ReadTstInfo(context.pTimeStamp);
        }

        public int Version => Decoded.Version;

        public string PolicyId => Decoded.PolicyId;

        public Oid HashAlgorithmId
        {
            get
            {
                Oid value = Decoded.HashAlgorithmId;

                return new Oid(value.Value, value.FriendlyName);
            }
        }

        public byte[] GetMessageHash()
        {
            return (byte[])Decoded.HashedMessage.Clone();
        }

        public bool HasMessageHash(byte[] hash)
        {
            if (hash == null)
                return false;

            byte[] value = Decoded.HashedMessage;

            if (hash.Length != value.Length)
            {
                return false;
            }

            return value.SequenceEqual(hash);
        }

        /// <summary>
        /// Gets the serial number for the request in the big-endian byte order.
        /// </summary>
        public byte[] GetSerialNumber()
        {
            return (byte[])Decoded.SerialNumber.Clone();
        }

        public DateTimeOffset Timestamp => Decoded.Timestamp;

        public long? AccuracyInMicroseconds => Decoded.AccuracyInMicroseconds;

        public bool IsOrdering => Decoded.IsOrdering;

        public byte[] GetNonce()
        {
            return (byte[])Decoded.Nonce?.Clone();
        }

        public byte[] GetTimestampAuthorityName()
        {
            return (byte[])Decoded.TsaName?.Clone();
        }

        public bool HasExtensions => Decoded.Extensions != null;

        public X509ExtensionCollection GetExtensions()
        {
            return ShallowCopy(Decoded.Extensions, preserveNull: false);
        }

        internal static X509ExtensionCollection ShallowCopy(X509ExtensionCollection existing, bool preserveNull)
        {
            if (preserveNull && existing == null)
                return null;

            var coll = new X509ExtensionCollection();

            if (existing == null)
                return coll;

            foreach (var extn in existing)
            {
                coll.Add(extn);
            }

            return coll;
        }

        public override void CopyFrom(AsnEncodedData asnEncodedData)
        {
            _decoded = null;
            base.CopyFrom(asnEncodedData);
        }

        private static TstInfo ReadTstInfo(IntPtr pTstInfo)
        {
            var info = (Rfc3161TimestampWin32.CRYPT_TIMESTAMP_INFO)Marshal.PtrToStructure(pTstInfo, typeof(Rfc3161TimestampWin32.CRYPT_TIMESTAMP_INFO));

            TstInfo tstInfo = new TstInfo
            {
                Version = info.dwVersion,
                PolicyId = Marshal.PtrToStringAnsi(info.pszTSAPolicyId),
                HashedMessage = CopyFromNative(ref info.HashedMessage),
                SerialNumber = CopyFromNative(ref info.SerialNumber),
                IsOrdering = info.fOrdering,
                Nonce = CopyFromNative(ref info.Nonce),
                TsaName = CopyFromNative(ref info.Tsa),
            };

            // Convert to BigEndian.
            Array.Reverse(tstInfo.SerialNumber);

            string hashAlgOidValue = Marshal.PtrToStringAnsi(info.HashAlgorithm.pszOid);
            Oid hashAlgOid;

            try
            {
                hashAlgOid = Oid.FromOidValue(hashAlgOidValue, OidGroup.HashAlgorithm);
            }
            catch (CryptographicException)
            {
                hashAlgOid = new Oid(hashAlgOidValue, hashAlgOidValue);
            }

            tstInfo.HashAlgorithmId = hashAlgOid;

            long filetime = info.ftTime.dwHighDateTime;
            filetime <<= 32;
            filetime |= (info.ftTime.dwLowDateTime & 0xFFFFFFFFL);

            tstInfo.Timestamp = DateTimeOffset.FromFileTime(filetime).ToUniversalTime();

            if (info.pvAccuracy != IntPtr.Zero)
            {
                var accuracy = (Rfc3161TimestampWin32.CRYPT_TIMESTAMP_ACCURACY)Marshal.PtrToStructure(info.pvAccuracy, typeof(Rfc3161TimestampWin32.CRYPT_TIMESTAMP_ACCURACY));

                long accuracyMicroSeconds =
                    accuracy.dwSeconds * 1_000_000L +
                    accuracy.dwMillis * 1000L +
                    accuracy.dwSeconds;

                tstInfo.AccuracyInMicroseconds = accuracyMicroSeconds;
            }

            if (info.cExtension > 0)
            {
                throw new NotImplementedException();
            }

            if (tstInfo.HashedMessage == null || tstInfo.SerialNumber == null)
            {
                throw new CryptographicException();
            }

            return tstInfo;
        }

        private static unsafe TstInfo Decode(byte[] rawData)
        {
            byte[] really = new byte[rawData.Length];

            fixed (byte* pbData = rawData)
            {
                IntPtr decodedPtr = IntPtr.Zero;
                int cbStruct = 0;

                try
                {
                    if (!Rfc3161TimestampWin32.CryptDecodeObjectEx(
                        Rfc3161TimestampWin32.CryptEncodingTypes.X509_ASN_ENCODING,
                        Rfc3161TimestampWin32.TIMESTAMP_INFO,
                        (IntPtr)pbData,
                        rawData.Length,
                        Rfc3161TimestampWin32.CryptDecodeObjectFlags.CRYPT_DECODE_ALLOC_FLAG |
                            Rfc3161TimestampWin32.CryptDecodeObjectFlags.CRYPT_DECODE_NOCOPY_FLAG |
                            Rfc3161TimestampWin32.CryptDecodeObjectFlags.CRYPT_DECODE_NO_SIGNATURE_BYTE_REVERSAL_FLAG,
                        IntPtr.Zero,
                        (IntPtr)(&decodedPtr),
                        ref cbStruct))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }

                    TstInfo tstInfo = ReadTstInfo(decodedPtr);

                    //fixed (byte* pbReally = really)
                    {
                        uint cbReally = 0;
                        IntPtr letThemEncodeIt = IntPtr.Zero;

                        if (!Rfc3161TimestampWin32.CryptEncodeObjectEx(
                            Rfc3161TimestampWin32.CryptEncodingTypes.X509_ASN_ENCODING,
                            Rfc3161TimestampWin32.TIMESTAMP_INFO,
                            decodedPtr,
                            Rfc3161TimestampWin32.CryptEncodeObjectFlags.CRYPT_ENCODE_ALLOC_FLAG,
                            IntPtr.Zero,
                            (IntPtr)(&letThemEncodeIt),
                            ref cbReally))
                        {
                            throw new CryptographicException(Marshal.GetLastWin32Error());
                        }

                        really = new byte[cbReally];
                        Marshal.Copy(letThemEncodeIt, really, 0, (int)cbReally);
                    }

                    return tstInfo;
                }
                finally
                {
                    if (decodedPtr != IntPtr.Zero)
                        Rfc3161TimestampWin32.LocalFree(decodedPtr);
                }
            }
        }

        private static unsafe byte[] Encode(TstInfo tstInfo)
        {
            string algOid = tstInfo.HashAlgorithmId.Value;

            byte[] policyOidBytes = new byte[tstInfo.PolicyId.Length + 1];
            byte[] algOidBytes = new byte[algOid.Length + 1];
            byte[] serialNumberBigEndian = tstInfo.SerialNumber;
            byte[] serialNumberLittleEndian = new byte[serialNumberBigEndian.Length];

            for (int i = 0; i < serialNumberLittleEndian.Length; i++)
            {
                serialNumberLittleEndian[i] = serialNumberBigEndian[serialNumberBigEndian.Length - i - 1];
            }

            long filetime = tstInfo.Timestamp.ToFileTime();

            fixed (byte* pbPolicyOid = policyOidBytes)
            fixed (char* pszPolicyOid = tstInfo.PolicyId)
            fixed (byte* pbHashedMessage = tstInfo.HashedMessage)
            fixed (byte* pbSerialNumber = serialNumberLittleEndian)
            fixed (char* pszAlgOid = algOid)
            fixed (byte* pbAlgOid = algOidBytes)
            fixed (byte* pbNonce = tstInfo.Nonce)
            fixed (byte* pbTsaName = tstInfo.TsaName)
            {
                Encoding.ASCII.GetBytes(pszPolicyOid, tstInfo.PolicyId.Length, pbPolicyOid, policyOidBytes.Length);
                Encoding.ASCII.GetBytes(pszAlgOid, algOid.Length, pbAlgOid, algOidBytes.Length);

                Rfc3161TimestampWin32.CRYPT_TIMESTAMP_INFO info = new Rfc3161TimestampWin32.CRYPT_TIMESTAMP_INFO
                {
                    dwVersion = tstInfo.Version,
                    pszTSAPolicyId = (IntPtr)pbPolicyOid,
                    fOrdering = tstInfo.IsOrdering,
                };

                Rfc3161TimestampWin32.CRYPT_TIMESTAMP_ACCURACY accuracy = default(Rfc3161TimestampWin32.CRYPT_TIMESTAMP_ACCURACY);

                info.HashAlgorithm.pszOid = (IntPtr)pbAlgOid;
                info.HashedMessage.cbData = (uint)tstInfo.HashedMessage.Length;
                info.HashedMessage.pbData = (IntPtr)pbHashedMessage;
                info.SerialNumber.cbData = (uint)serialNumberLittleEndian.Length;
                info.SerialNumber.pbData = (IntPtr)pbSerialNumber;

                info.ftTime.dwLowDateTime = (int)(filetime & 0xFFFFFFFF);
                info.ftTime.dwHighDateTime = (int)(filetime >> 32);

                if (tstInfo.AccuracyInMicroseconds.HasValue)
                {
                    long val = tstInfo.AccuracyInMicroseconds.Value;
                    long rem;
                    val = Math.DivRem(val, 1000, out rem);
                    accuracy.dwMicros = (int)rem;
                    val = Math.DivRem(val, 1000, out rem);
                    accuracy.dwMillis = (int)rem;

                    if (val > int.MaxValue)
                    {
                        Debug.Fail($"accuracy value {tstInfo.AccuracyInMicroseconds.Value} had seconds component {val}, which should have been stopped");
                        throw new CryptographicException();
                    }

                    accuracy.dwSeconds = (int)val;
                    info.pvAccuracy = (IntPtr)(&accuracy);
                }

                if (tstInfo.Nonce != null)
                {
                    info.Nonce.cbData = (uint)tstInfo.Nonce.Length;
                    info.Nonce.pbData = (IntPtr)pbNonce;
                }

                if (tstInfo.TsaName != null)
                {
                    info.Tsa.cbData = (uint)tstInfo.TsaName.Length;
                    info.Tsa.pbData = (IntPtr)pbTsaName;
                }

                if (tstInfo.Extensions != null)
                    throw new NotImplementedException();

                IntPtr encodedDataPtr = IntPtr.Zero;
                uint cbEncoded = 0;

                try
                {
                    if (!Rfc3161TimestampWin32.CryptEncodeObjectEx(
                        Rfc3161TimestampWin32.CryptEncodingTypes.X509_ASN_ENCODING,
                        Rfc3161TimestampWin32.TIMESTAMP_INFO,
                        (IntPtr)(&info),
                        Rfc3161TimestampWin32.CryptEncodeObjectFlags.CRYPT_ENCODE_ALLOC_FLAG,
                        IntPtr.Zero,
                        (IntPtr)(&encodedDataPtr),
                        ref cbEncoded))
                    {
                        throw new CryptographicException(Marshal.GetLastWin32Error());
                    }

                    byte[] encoded = new byte[cbEncoded];
                    Marshal.Copy(encodedDataPtr, encoded, 0, (int)cbEncoded);
                    return encoded;
                }
                finally
                {
                    if (encodedDataPtr != IntPtr.Zero)
                        Rfc3161TimestampWin32.LocalFree(encodedDataPtr);
                }
            }
        }

        internal static byte[] CopyFromNative(ref Rfc3161TimestampWin32.CRYPTOAPI_BLOB blob)
        {
            if (blob.cbData == 0)
                return null;

            byte[] answer = new byte[blob.cbData];
            Marshal.Copy(blob.pbData, answer, 0, answer.Length);
            return answer;
        }

    }
}
