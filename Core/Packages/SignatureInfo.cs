using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NuGet.Packaging.Signing;

namespace NuGetPe
{
    public class SignatureInfo
    {
        private readonly Signature signature;

        public SignatureInfo(Signature signature)
        {
            this.signature = signature;
            var ts = GetTimestamp();
            Timestamp = ts.Item1;
            TimestampSignerInfo = ts.Item2;
        }
        
        public SignerInfo SignerInfo => signature.SignerInfo;

        public SignatureType Type => signature.Type;

        public DateTimeOffset? Timestamp { get; }

        public SignerInfo TimestampSignerInfo {get;}


        private (DateTimeOffset?, SignerInfo) GetTimestamp()
        {
            var authorUnsignedAttributes = signature.SignerInfo.UnsignedAttributes;
            var timestampCms = new SignedCms();

            foreach (var attribute in authorUnsignedAttributes)
            {
                if (string.Equals(attribute.Oid.Value, Oids.SignatureTimeStampTokenAttributeOid))
                {
                    timestampCms.Decode(attribute.Values[0].RawData);

                    if (Rfc3161TimestampVerificationUtility.TryReadTSTInfoFromSignedCms(timestampCms, out var tstInfo))
                    {
                        return (tstInfo.Timestamp, timestampCms.SignerInfos[0]);
                    }
                }
            }

            return (null, null);
        }
    }
}
