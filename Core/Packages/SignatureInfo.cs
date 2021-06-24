using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

using NuGet.Packaging.Signing;

using TBSCertificate = NuGetPe.Packages.Pkcs.TBSCertificate;

namespace NuGetPe
{
    public class SignatureInfo
    {
#if IS_SIGNING_SUPPORTED
        private readonly Signature? _signature;

        public SignatureInfo(Signature signature)
        {
            _signature = signature ?? throw new ArgumentNullException(nameof(signature));

            try
            {
                Type = signature.Type;
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections. Instead use the collection directly
                var ts = signature.Timestamps.FirstOrDefault();
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections. Instead use the collection directly

                Timestamp = ts?.GeneralizedTime;
                TimestampSignerInfo = ts?.SignerInfo;
            }
            catch (CryptographicException) // possibly a malformed timestamp
            {
            }
        }
#endif

        /// <remarks>
        /// Alternative ctor without any dependency to System.Security.Cryptography namespace
        /// note: IS_SIGNING_SUPPORTED isnt accurate here.
        /// As wasm (local, not ci) uses .net 5.0, but doesnt support it.
        /// In fact, <see cref="AppCompat.IsSupported(RuntimeFeature)"/> should be used instead.
        /// </remarks>
        public SignatureInfo(
            TBSCertificate? signerCertificate,
            DateTimeOffset? timestamp,
            TBSCertificate? timestampSignerCertificate)
        {
            SignerCertificate = signerCertificate;
            Timestamp = timestamp;
            TimestampSignerCertificate = timestampSignerCertificate;
        }

        public SignerInfo? SignerInfo =>
#if !IS_SIGNING_SUPPORTED
            null;
#else
            _signature?.SignerInfo;
#endif

        public TBSCertificate? SignerCertificate { get; }

        public SignatureType Type { get; protected set; } = SignatureType.Unknown;

        public DateTimeOffset? Timestamp { get; }

        public SignerInfo? TimestampSignerInfo { get; }

        public TBSCertificate? TimestampSignerCertificate { get; }

        public string? FriendlyName =>
#if !IS_SIGNING_SUPPORTED
            "Unknown";
#else
            _signature?.FriendlyName;
#endif
    }

    public class PublisherSignatureInfo : SignatureInfo
    {
#if IS_SIGNING_SUPPORTED
        public PublisherSignatureInfo(Signature signature) : base(signature)
        {
        }
#endif

        // See remarks on base ctor
        public PublisherSignatureInfo(
            TBSCertificate? signerCertificate,
            DateTimeOffset? timestamp,
            TBSCertificate? timestampSignerCertificate)
            : base(signerCertificate, timestamp, timestampSignerCertificate)
        {
            Type = SignatureType.Author;
        }
    }

    public class RepositorySignatureInfo : SignatureInfo
    {
#if IS_SIGNING_SUPPORTED
        public RepositorySignatureInfo(Signature repositorySignature) : base(repositorySignature)
        {
            var signature = repositorySignature as IRepositorySignature ?? throw new ArgumentException("Not a repository signature", nameof(repositorySignature));

            V3ServiceIndexUrl = signature.V3ServiceIndexUrl;
            PackageOwners = signature.PackageOwners;
        }
#endif

        // See remarks on base ctor
        public RepositorySignatureInfo(
            TBSCertificate? signerCertificate,
            DateTimeOffset? timestamp,
            TBSCertificate? timestampSignerCertificate,
            Uri? v3ServiceIndexUrl,
            IReadOnlyList<string>? packageOwners)
            : base(signerCertificate, timestamp, timestampSignerCertificate)
        {
            Type = SignatureType.Repository;

            V3ServiceIndexUrl = v3ServiceIndexUrl;
            PackageOwners = packageOwners ?? Array.Empty<string>();
        }

        public Uri? V3ServiceIndexUrl { get; }

        public IReadOnlyList<string> PackageOwners { get; }
    }
}
