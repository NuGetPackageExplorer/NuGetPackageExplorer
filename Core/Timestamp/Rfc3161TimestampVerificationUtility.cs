// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

using System.Security.Cryptography.Pkcs;

using System.Security.Cryptography.X509Certificates;

namespace NuGet.Packaging.Signing
{
    /// <summary>
    /// Provides convinience method for verification of a RFC 3161 Timestamp.
    /// </summary>
    internal static class Rfc3161TimestampVerificationUtility
    {
        private const double _millisecondsPerMicrosecond = 0.001;


        internal static bool ValidateSignerCertificateAgainstTimestamp(
            X509Certificate2 signerCertificate,
            Rfc3161TimestampTokenInfo tstInfo)
        {
            var tstInfoGenTime = tstInfo.Timestamp;
            var accuracyInMilliseconds = GetAccuracyInMilliseconds(tstInfo);

            var timestampUpperGenTime = tstInfoGenTime.AddMilliseconds(accuracyInMilliseconds);
            var timestampLowerGenTime = tstInfoGenTime.Subtract(TimeSpan.FromMilliseconds(accuracyInMilliseconds));

            DateTimeOffset signerCertExpiry = DateTime.SpecifyKind(signerCertificate.NotAfter, DateTimeKind.Local);
            DateTimeOffset signerCertBegin = DateTime.SpecifyKind(signerCertificate.NotBefore, DateTimeKind.Local);

            return timestampUpperGenTime < signerCertExpiry &&
                timestampLowerGenTime > signerCertBegin;
        }

        internal static bool TryReadTSTInfoFromSignedCms(
            SignedCms timestampCms,
            out Rfc3161TimestampTokenInfo tstInfo)
        {
            tstInfo = null;
            if (timestampCms.ContentInfo.ContentType.Value.Equals(Oids.TSTInfoContentTypeOid))
            {
                tstInfo = new Rfc3161TimestampTokenInfo(timestampCms.ContentInfo.Content);
                return true;
            }
            // return false if the signedCms object does not contain the right ContentType
            return false;
        }

        internal static DateTimeOffset GetUpperLimit(SignedCms timestampCms)
        {
            var result = DateTimeOffset.Now;
            if (TryReadTSTInfoFromSignedCms(timestampCms, out var tstInfo))
            {
                var accuracyInMilliseconds = GetAccuracyInMilliseconds(tstInfo);
                return tstInfo.Timestamp.AddMilliseconds(accuracyInMilliseconds);
            }
            return result;
        }

        internal static double GetAccuracyInMilliseconds(Rfc3161TimestampTokenInfo tstInfo)
        {
            double accuracyInMilliseconds;

            if (!tstInfo.AccuracyInMicroseconds.HasValue)
            {
                if (string.Equals(tstInfo.PolicyId, Oids.BaselineTimestampPolicyOid))
                {
                    accuracyInMilliseconds = 1000;
                }
                else
                {
                    accuracyInMilliseconds = 0;
                }
            }
            else
            {
                accuracyInMilliseconds = tstInfo.AccuracyInMicroseconds.Value * _millisecondsPerMicrosecond;
            }

            if (accuracyInMilliseconds < 0)
            {
                throw new InvalidDataException("Invalid timestamp data");
            }

            return accuracyInMilliseconds;
        }
    }
}
