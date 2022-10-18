using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc3126
// impl reference: https://github.com/NuGet/NuGet.Client/blob/e89d16985a9d28c837e13369753eac6ed1f952a3/src/NuGet.Core/NuGet.Packaging/Signing/Signatures/CommitmentTypeQualifier.cs

namespace NuGetPe.Packages.Pkcs
{
    public class CommitmentTypeQualifier
    {
        public string CommitmentTypeIdentifier { get; private set; }
        public ReadOnlyMemory<byte>? Qualifier { get; private set; }

        public static CommitmentTypeQualifier Decode(AsnReader reader)
        {
            /* CommitmentTypeQualifier ::= SEQUENCE {
                commitmentTypeIdentifier   CommitmentTypeIdentifier,
                qualifier                  ANY DEFINED BY commitmentTypeIdentifier }*/
            var sequence = reader.ReadSequence();
            var commitmentTypeIdentifier = sequence.ReadObjectIdentifier();
            var qualifier = sequence.ReadOptional(null, (r, _) => r, sequence => sequence.ReadEncodedValue());

            sequence.ThrowIfNotEmpty();
            return new()
            {
                CommitmentTypeIdentifier = commitmentTypeIdentifier,
                Qualifier = qualifier,
            };
        }
    }
}
