using System;
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
    }
}
