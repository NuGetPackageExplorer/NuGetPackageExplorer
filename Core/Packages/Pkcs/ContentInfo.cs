using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src
//      ^ modified

namespace NuGetPe.Packages.Pkcs
{
    public class ContentInfo
    {
        public string ContentType { get; private set; }
        public ReadOnlyMemory<byte> Content { get; private set; }

        public static ContentInfo Decode(ReadOnlyMemory<byte> data) => Decode(new AsnReader(data, AsnEncodingRules.BER));
        public static ContentInfo Decode(AsnReader reader)
        {
            /* ContentInfo ::= SEQUENCE {
                contentType ContentType,
                content [0] EXPLICIT ANY DEFINED BY contentType } */
            var sequence = reader.ReadSequence(Asn1Tag.Sequence);
            var contentType = sequence.ReadObjectIdentifier();

            var reader2 = sequence.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 0));
            var content = reader2.ReadEncodedValue();

            reader2.ThrowIfNotEmpty();
            sequence.ThrowIfNotEmpty();

            return new()
            {
                ContentType = contentType,
                Content = content,
            };
        }

        public static T Unwrap<T>(string expectedTypeID, ReadOnlyMemory<byte> data, Func<ReadOnlyMemory<byte>, T> parse)
        {
            var content = Decode(data);
            if (content.ContentType != expectedTypeID)
            {
                throw new CryptographicException("Cryptography_Cms_InvalidMessageType");
            }

            return parse(content.Content);
        }
    }
}
