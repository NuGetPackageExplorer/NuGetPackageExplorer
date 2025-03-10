using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

using NuGet.Packaging.Signing;

using TBSCertificate = NuGetPe.Packages.Pkcs.TBSCertificate;

namespace NuGetPe
{
    public class SignatureInfo
    {
        private readonly Signature? _signature;

        public SignatureInfo(Signature? signature)
        {
            if (!AppCompat.IsSupported(RuntimeFeature.Cryptography)) return;

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

        public SignatureInfo(
            TBSCertificate? signerCertificate,
            DateTimeOffset? timestamp,
            TBSCertificate? timestampSignerCertificate)
        {
            SignerCertificate = signerCertificate;
            Timestamp = timestamp;
            TimestampSignerCertificate = timestampSignerCertificate;
        }

        public SignerInfo? SignerInfo => AppCompat.IsSupported(RuntimeFeature.Cryptography) ?
            _signature?.SignerInfo :
            null;

        public TBSCertificate? SignerCertificate { get; }

        public SignatureType Type { get; protected set; } = SignatureType.Unknown;

        public DateTimeOffset? Timestamp { get; }

        public SignerInfo? TimestampSignerInfo { get; }

        public TBSCertificate? TimestampSignerCertificate { get; }

        public string? FriendlyName => AppCompat.IsSupported(RuntimeFeature.Cryptography) ?
            _signature?.FriendlyName :
            "Unknown";
    }

    public class PublisherSignatureInfo : SignatureInfo
    {
        public PublisherSignatureInfo(Signature signature) : base(signature)
        {
        }

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
        public RepositorySignatureInfo(Signature? repositorySignature) : base(repositorySignature)
        {
            if (!AppCompat.IsSupported(RuntimeFeature.Cryptography)) return;

            var signature = repositorySignature as IRepositorySignature ?? throw new ArgumentException("Not a repository signature", nameof(repositorySignature));

            V3ServiceIndexUrl = signature.V3ServiceIndexUrl;
            PackageOwners = signature.PackageOwners;
        }

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
            PackageOwners = packageOwners ?? [];
        }

        public Uri? V3ServiceIndexUrl { get; }

        public IReadOnlyList<string> PackageOwners { get; }
    }
}
