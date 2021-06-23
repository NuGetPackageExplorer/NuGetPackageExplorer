using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.Packages.Pkcs
{
    /// <remarks>Renamed to AttributeValue to avoid mixing up with System.Attribute</remarks>
    public class AttributeValue
    {
        public string AttrType { get; private set; }
        public ReadOnlyMemory<byte>[] AttrValues { get; private set; }

        public static AttributeValue Decode(AsnReader reader)
        {
            /* Attribute ::= SEQUENCE {
                attrType OBJECT IDENTIFIER,
                attrValues SET OF AttributeValue }*/
            var sequence = reader.ReadSequence();
            var attrType = sequence.ReadObjectIdentifier();
            var attrValues = sequence.ReadSetOf(x => x.ReadEncodedValue());

            sequence.ThrowIfNotEmpty();
            return new()
            {
                AttrType = attrType,
                AttrValues = attrValues,
            };
        }
    }

    public static class AttributeExtensions
    {
        public static AttributeValue? GetAttribute(this IEnumerable<AttributeValue> sources, string oid)
        {
            var attributes = sources.Where(x => x.AttrType == oid).ToArray();
            var attribute = attributes.Length switch
            {
                0 => null,
                1 => attributes[0],
                _ => throw new CryptographicException($"MultipleAttributesDisallowed: {oid}"),
            };

            return attribute;
        }
    }
}
