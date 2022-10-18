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
    public class AlgorithmIdentifier
    {
        public string Algorithm { get; private set; }
        public ReadOnlyMemory<byte>? Parameters { get; private set; }

        public static AlgorithmIdentifier Decode(AsnReader reader)
        {
            /* AlgorithmIdentifier  ::=  SEQUENCE  {
                algorithm               OBJECT IDENTIFIER,
                parameters              ANY DEFINED BY algorithm OPTIONAL  }*/
            var sequence = reader.ReadSequence();
            var algorithm = sequence.ReadObjectIdentifier();
            var parameters = sequence.ReadOptional(null, (r, _) => r, reader => reader.ReadEncodedValue());

            sequence.ThrowIfNotEmpty();
            return new()
            {
                Algorithm = algorithm,
                Parameters = parameters,
            };
        }
    }
}
