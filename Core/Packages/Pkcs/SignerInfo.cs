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
    public class SignerInfo
    {
        public int Version { get; private set; }
        public SignerIdentifier Sid { get; private set; }
        public AlgorithmIdentifier DigestAlgorithm { get; private set; }
        public AttributeValue[]? SignedAttrs { get; private set; }
        public AlgorithmIdentifier SignatureAlgorithm { get; private set; }
        public ReadOnlyMemory<byte> Signature { get; private set; }
        public AttributeValue[]? UnsignedAttrs { get; private set; }

        public static SignerInfo Decode(ReadOnlyMemory<byte> data) => Decode(new AsnReader(data, AsnEncodingRules.BER));
        public static SignerInfo Decode(AsnReader reader)
        {
            /* SignerInfo ::= SEQUENCE {
                version CMSVersion,
                sid SignerIdentifier,
                digestAlgorithm DigestAlgorithmIdentifier,
                signedAttrs [0] IMPLICIT SignedAttributes OPTIONAL,
                signatureAlgorithm SignatureAlgorithmIdentifier,
                signature SignatureValue,
                unsignedAttrs [1] IMPLICIT UnsignedAttributes OPTIONAL }*/
            var sequence = reader.ReadSequence();

            // version CMSVersion,
            if (!sequence.TryReadInt32(out var version))
            {
                sequence.ThrowIfNotEmpty();
            }

            // sid SignerIdentifier,
            var sid = SignerIdentifier.Decode(sequence, validateEmpty: false);

            // digestAlgorithm DigestAlgorithmIdentifier,
            var digestAlgorithm = AlgorithmIdentifier.Decode(sequence);

            // signedAttrs [0] IMPLICIT SignedAttributes OPTIONAL,
            // SignedAttributes ::= SET SIZE (1..MAX) OF Attribute
            var signedAttrs = sequence.ReadOptionalSetOf(0, set => AttributeValue.Decode(set));

            // signatureAlgorithm SignatureAlgorithmIdentifier,
            var signatureAlgorithm = AlgorithmIdentifier.Decode(sequence);

            // signature SignatureValue,
            // SignatureValue ::= OCTET STRING
            var signature = sequence.ReadPrimitiveOrNotOctetString();

            // unsignedAttrs [1] IMPLICIT UnsignedAttributes OPTIONAL
            // UnsignedAttributes ::= SET SIZE (1..MAX) OF Attribute
            var unsignedAttrs = sequence.ReadOptionalSetOf(1, set => AttributeValue.Decode(set));

            sequence.ThrowIfNotEmpty();
            return new()
            {
                Version = version,
                Sid = sid,
                DigestAlgorithm = digestAlgorithm,
                SignedAttrs = signedAttrs,
                SignatureAlgorithm = signatureAlgorithm,
                Signature = signature,
                UnsignedAttrs = unsignedAttrs,
            };
        }
    }
}
