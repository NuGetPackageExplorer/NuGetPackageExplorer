using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using NuGet.Packaging.Signing;

namespace NuGetPe
{
    public class SignatureInfo
    {
        private readonly Signature signature;

        public SignatureInfo(Signature signature)
        {
            this.signature = signature;
            var ts = signature.Timestamps.FirstOrDefault();
            Timestamp = ts?.GeneralizedTime;
            TimestampSignerInfo = ts?.SignerInfo;
        }

        public SignerInfo SignerInfo => signature.SignerInfo;

        public SignatureType Type => signature.Type;

        public DateTimeOffset? Timestamp { get; }

        public SignerInfo TimestampSignerInfo { get; }

        public string FriendlyName => signature.FriendlyName;
    }

    public class RepositorySignatureInfo : SignatureInfo
    {
        private readonly IRepositorySignature repositorySignature;
        public RepositorySignatureInfo(Signature repositorySignature) : base(repositorySignature)
        {
            this.repositorySignature = repositorySignature as IRepositorySignature ?? throw new ArgumentException("Not a repository signature", nameof(repositorySignature));
        }

        public Uri V3ServiceIndexUrl => repositorySignature.V3ServiceIndexUrl;

        public IReadOnlyList<string> PackageOwners => repositorySignature.PackageOwners;
    }
}
