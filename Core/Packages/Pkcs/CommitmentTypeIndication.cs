using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc3126
// impl reference: https://github.com/NuGet/NuGet.Client/blob/e89d16985a9d28c837e13369753eac6ed1f952a3/src/NuGet.Core/NuGet.Packaging/Signing/Signatures/CommitmentTypeIndication.cs

namespace NuGetPe.Packages.Pkcs
{
    public class CommitmentTypeIndication
    {
        public string CommitmentTypeId { get; private set; }
        public CommitmentTypeQualifier[]? CommitmentTypeQualifiers { get; private set; }

        public static CommitmentTypeIndication Decode(ReadOnlyMemory<byte> data) => Decode(new AsnReader(data, AsnEncodingRules.BER));
        public static CommitmentTypeIndication Decode(AsnReader reader)
        {
            /*CommitmentTypeIndication ::= SEQUENCE {
               commitmentTypeId            CommitmentTypeIdentifier,
               commitmentTypeQualifier     SEQUENCE SIZE (1..MAX) OF CommitmentTypeQualifier OPTIONAL}*/
            var sequence = reader.ReadSequence();
            var commitmentTypeId = sequence.ReadObjectIdentifier();
            var commitmentTypeQualifiers = sequence.ReadOptionalSequenceOf(null, sequence => CommitmentTypeQualifier.Decode(sequence));

            sequence.ThrowIfNotEmpty();
            return new()
            {
                CommitmentTypeId = commitmentTypeId,
                CommitmentTypeQualifiers = commitmentTypeQualifiers,
            };
        }
    }
}
