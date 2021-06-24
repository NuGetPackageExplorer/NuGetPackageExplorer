using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5652
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src
//      ^ modified

namespace NuGetPe.Packages.Pkcs
{
    public class SignedData
    {
        public const string ContentTypeID = "1.2.840.113549.1.7.2";

        public int Version { get; private set; }
        public AlgorithmIdentifier[] DigestAlgorithms { get; private set; }
        public EncapsulatedContentInfo EncapContentInfo { get; private set; }
        public CertificateChoices[]? Certificates { get; private set; }
        public ReadOnlyMemory<byte>[] Crls { get; private set; }
        public SignerInfo[] SignerInfos { get; private set; }

        public static SignedData Decode(ReadOnlyMemory<byte> data) => Decode(new AsnReader(data, AsnEncodingRules.BER));
        public static SignedData Decode(AsnReader reader)
        {
            /* SignedData ::= SEQUENCE {
                version CMSVersion,
                digestAlgorithms DigestAlgorithmIdentifiers,
                encapContentInfo EncapsulatedContentInfo,
                certificates [0] IMPLICIT CertificateSet OPTIONAL,
                crls [1] IMPLICIT RevocationInfoChoices OPTIONAL,
                signerInfos SignerInfos }*/
            var sequence = reader.ReadSequence();

            // version CMSVersion,
            if (!sequence.TryReadInt32(out var version))
            {
                sequence.ThrowIfNotEmpty();
            }

            // digestAlgorithms DigestAlgorithmIdentifiers
            // DigestAlgorithmIdentifiers ::= SET OF DigestAlgorithmIdentifier
            var digestAlgorithms = sequence.ReadSetOf(set => AlgorithmIdentifier.Decode(set));

            // encapContentInfo EncapsulatedContentInfo
            var encapContentInfo = EncapsulatedContentInfo.Decode(sequence);

            // certificates [0] IMPLICIT CertificateSet OPTIONAL,
            var certificates = sequence.ReadOptionalSetOf(0, set => CertificateChoices.Decode(set));

            // crls [1] IMPLICIT RevocationInfoChoices OPTIONAL,
            var crls = sequence.ReadOptionalSetOf(1, set => set.ReadEncodedValue());

            // signerInfos SignerInfos
            // SignerInfos ::= SET OF SignerInfo
            var signerInfos = sequence.ReadSetOf(set => SignerInfo.Decode(set));

            sequence.ThrowIfNotEmpty();
            return new()
            {
                Version = version,
                DigestAlgorithms = digestAlgorithms,
                EncapContentInfo = encapContentInfo,
                Certificates = certificates,
                Crls = crls,
                SignerInfos = signerInfos,
            };
        }
    }
}
