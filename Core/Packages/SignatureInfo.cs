using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using NuGet.Packaging.Signing;

namespace NuGetPe
{
    public class SignatureInfo
    {
        private readonly Signature _signature;

        public SignatureInfo(Signature signature)
        {
            _signature = signature ?? throw new ArgumentNullException(nameof(signature));
#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections. Instead use the collection directly
            var ts = signature.Timestamps.FirstOrDefault();
#pragma warning restore CA1826 // Do not use Enumerable methods on indexable collections. Instead use the collection directly
            Timestamp = ts?.GeneralizedTime;
            TimestampSignerInfo = ts?.SignerInfo;
        }

        public SignerInfo SignerInfo => _signature.SignerInfo;

        public SignatureType Type => _signature.Type;

        public DateTimeOffset? Timestamp { get; }

        public SignerInfo? TimestampSignerInfo { get; }

        public string FriendlyName => _signature.FriendlyName;
    }

    public class RepositorySignatureInfo : SignatureInfo
    {
        private readonly IRepositorySignature _repositorySignature;
        public RepositorySignatureInfo(Signature repositorySignature) : base(repositorySignature)
        {
            _repositorySignature = repositorySignature as IRepositorySignature ?? throw new ArgumentException("Not a repository signature", nameof(repositorySignature));
        }

        public Uri V3ServiceIndexUrl => _repositorySignature.V3ServiceIndexUrl;

        public IReadOnlyList<string> PackageOwners => _repositorySignature.PackageOwners;
    }
}
