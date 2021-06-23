using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src

namespace NuGetPe.Packages.Pkcs
{
    public class EncapsulatedContentInfo
    {
        public string ContentType { get; private set; }
        public ReadOnlyMemory<byte> Content { get; private set; }

        public static EncapsulatedContentInfo Decode(AsnReader reader)
        {
            /* EncapsulatedContentInfo ::= SEQUENCE {
                eContentType ContentType,
                eContent [0] EXPLICIT OCTET STRING OPTIONAL } */
            var sequence = reader.ReadSequence();
            var contentType = sequence.ReadObjectIdentifier();
            var content = sequence.ReadOptionalSequence(0, sequence => sequence.ReadEncodedValue());

            sequence.ThrowIfNotEmpty();
            return new()
            {
                ContentType = contentType,
                Content = content,
            };
        }
    }
}
