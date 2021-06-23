using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using NuGet.Packaging.Signing;

// docs reference: https://tools.ietf.org/html/rfc5280
// impl reference: n/a

namespace NuGetPe.Packages.Pkcs
{
    public class TBSCertificateExtension
    {
        public string ExtnID { get; private set; }
        public bool Critical { get; private set; }
        public byte[] ExtnValue { get; private set; }

        public static TBSCertificateExtension Decode(AsnReader reader)
        {
            /* 	Extension  ::=  SEQUENCE  {
                    extnID      OBJECT IDENTIFIER,
                    critical    BOOLEAN DEFAULT FALSE,
                    extnValue   OCTET STRING
                                -- contains the DER encoding of an ASN.1 value
                                -- corresponding to the extension type identified
                                -- by extnID } */
            var sequence = reader.ReadSequence();
            var extnID = sequence.ReadObjectIdentifier();
            var critical = sequence.ReadOptional<bool?>(null, (x, t) => t.HasSameClassAndValue(Asn1Tag.Boolean) ? x.ReadBoolean() : default) ?? false;
            var extnValue = sequence.ReadOctetString();

            sequence.ThrowIfNotEmpty();
            return new()
            {
                ExtnID = extnID,
                Critical = critical,
                ExtnValue = extnValue,
            };
        }
        public ReadOnlyMemory<byte> GetSubjectKeyIdentifier()
        {
            if (ExtnID != Oids.SubjectKeyIdentifier) throw new InvalidOperationException($"Invalid ExtnID: {ExtnID}");

            var reader = new AsnReader(ExtnValue, AsnEncodingRules.DER);
            if (reader.PeekTag().HasSameClassAndValue(Asn1Tag.PrimitiveOctetString))
            {
                var value = reader.ReadOctetString();

                reader.ThrowIfNotEmpty();
                return value;
            }

            throw new CryptographicException();
        }
    }
}
