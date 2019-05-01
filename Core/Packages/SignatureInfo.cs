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
            _signature = signature;
            var ts = signature.Timestamps.FirstOrDefault();
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
