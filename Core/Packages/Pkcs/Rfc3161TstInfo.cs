using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc3161
// impl reference: https://github.com/dotnet/runtime/tree/b02e13abab3d6f499751af2ad1ae6333fb4e633d/src/libraries/System.Security.Cryptography.Pkcs/src
//      ^ modified
namespace NuGetPe.Packages.Pkcs
{
    public class Rfc3161TstInfo
    {
        public const string ContentTypeID = "1.2.840.113549.1.9.16.2.14";

        public int Version { get; private set; }
        public string Policy { get; private set; }
        //public ReadOnlyMemory<byte> Messageimprint { get; private set; }
        public ReadOnlyMemory<byte> Serialnumber { get; private set; }
        public DateTimeOffset Gentime { get; private set; }
        //public Rfc3171Accuracy_ Accuracy { get; private set; }
        //public ReadOnlyMemory<byte> Ordering { get; private set; }
        //public ReadOnlyMemory<byte> Nonce { get; private set; }
        //public ReadOnlyMemory<byte> Tsa { get; private set; }
        //public ReadOnlyMemory<byte> Extensions { get; private set; }


        public static Rfc3161TstInfo From(EncapsulatedContentInfo content)
        {
            /*  EncapsulatedContentInfo ::= SEQUENCE {
                    eContentType ContentType,
                    eContent [0] EXPLICIT OCTET STRING OPTIONAL } */
            // note: the content field is read with ReadEncodedValue
            // because it would fail with ReadOctetString when reading the *.p7s.

            // just unwrap OCTET STRING node, [0] OPTIONAL is already stripped inside EncapsulatedContentInfo
            var reader = new AsnReader(content.Content, AsnEncodingRules.DER);
            var data = reader.ReadOctetString();

            return Decode(data);
        }
        public static Rfc3161TstInfo Decode(ReadOnlyMemory<byte> data) => Decode(new AsnReader(data, AsnEncodingRules.DER));
        public static Rfc3161TstInfo Decode(AsnReader reader)
        {
            /*  TSTInfo ::= SEQUENCE  {
                    version                      INTEGER  { v1(1) },
                    policy                       TSAPolicyId,
                    messageImprint               MessageImprint,
                        -- MUST have the same value as the similar field in
                        -- TimeStampReq
                    serialNumber                 INTEGER,
                        -- Time-Stamping users MUST be ready to accommodate integers
                        -- up to 160 bits.
                    genTime                      GeneralizedTime,
                    accuracy                     Accuracy                 OPTIONAL,
                    ordering                     BOOLEAN             DEFAULT FALSE,
                    nonce                        INTEGER                  OPTIONAL,
                        -- MUST be present if the similar field was present
                        -- in TimeStampReq.  In that case it MUST have the same value.
                    tsa                          [0] GeneralName          OPTIONAL,
                    extensions                   [1] IMPLICIT Extensions   OPTIONAL } */
            var sequence = reader.ReadSequence();

            // version                      INTEGER  { v1(1) },
            var version = sequence.ReadOrThrow<int>(null, sequence.TryReadInt32);

            // policy                       TSAPolicyId,
            // TSAPolicyId ::= OBJECT IDENTIFIER
            var policy = sequence.ReadObjectIdentifier();

            // messageImprint               MessageImprint,
            // serialNumber                 INTEGER,
            // genTime                      GeneralizedTime,
            var messageImprint = sequence.ReadEncodedValue();
            var serialNumber = sequence.ReadIntegerBytes();
            var genTime = sequence.ReadGeneralizedTime();

            // accuracy                     Accuracy                 OPTIONAL,
            // ordering                     BOOLEAN             DEFAULT FALSE,
            // nonce                        INTEGER                  OPTIONAL,
            // tsa                          [0] GeneralName          OPTIONAL,
            // extensions                   [1] IMPLICIT Extensions   OPTIONAL
            var accuracy = sequence.ReadOptional(Asn1Tag.Sequence, (r, t) => r.ReadEncodedValue());
            var ordering = sequence.ReadOptional(Asn1Tag.Boolean, (r, t) => r.ReadBoolean());
            var nonce = sequence.ReadOptional(null, (r, t) => r.ReadInteger());
            var tsa = sequence.ReadOptional(0, (r, t) => r.ReadEncodedValue());
            var extensions = sequence.ReadOptional(1, (r, t) => r.ReadEncodedValue());

            //sequence.ReadRemainingTags().Dump();
            sequence.ThrowIfNotEmpty();
            return new()
            {
                Version = version,
                Policy = policy,
                //Messageimprint = messageImprint,
                Serialnumber = serialNumber,
                Gentime = genTime,
                //Accuracy = accuracy,
                //Ordering = ordering, 
                //Nonce = nonce, 
                //Tsa = tsa, 
                //Extensions = extensions, 
            };
        }
    }
}
