using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src

namespace NuGetPe.Packages.Pkcs
{
    public class SignerIdentifier
    {
        public IssuerAndSerialNumber? IssuerAndSerialNumber { get; private set; }
        public ReadOnlyMemory<byte>? SubjectKeyIdentifier { get; set; }

        public static SignerIdentifier Decode(AsnReader reader, bool validateEmpty = true)
        {
            /* SignerIdentifier ::= CHOICE {
                issuerAndSerialNumber IssuerAndSerialNumber,
                subjectKeyIdentifier [0] SubjectKeyIdentifier } */
            var tag = reader.PeekTag();

            if (tag.HasSameClassAndValue(Asn1Tag.Sequence))
            {
                var result = new SignerIdentifier()
                {
                    IssuerAndSerialNumber = IssuerAndSerialNumber.Decode(reader),
                };

                if (validateEmpty) reader.ThrowIfNotEmpty();
                return result;
            }

            if (tag.HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                var result = new SignerIdentifier()
                {
                    SubjectKeyIdentifier = reader.ReadPrimitiveOrNotOctetString(tag),
                };

                if (validateEmpty) reader.ThrowIfNotEmpty();
                return result;
            }

            throw new CryptographicException();
        }
    }
}
