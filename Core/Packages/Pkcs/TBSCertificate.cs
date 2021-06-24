using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5280
// impl reference: n/a

namespace NuGetPe.Packages.Pkcs
{
    public class TBSCertificate
    {
        public int Version { get; private set; }
        public BigInteger SerialNumber { get; private set; }
        public AlgorithmIdentifier Signature { get; private set; }
        public DistinguishedName Issuer { get; private set; }
        public ReadOnlyMemory<byte> Validity { get; private set; }
        public DistinguishedName Subject { get; private set; }
        public ReadOnlyMemory<byte> SubjectPublicKeyInfo { get; private set; }
        public ReadOnlyMemory<byte> IssuerUniqueID { get; private set; }
        public ReadOnlyMemory<byte> SubjectUniqueID { get; private set; }
        public TBSCertificateExtension[]? Extensions { get; private set; }

        public static TBSCertificate Decode(AsnReader reader)
        {
            // https://tools.ietf.org/html/rfc5280
            /*  TBSCertificate  ::=  SEQUENCE  {
                    version         [0]  EXPLICIT Version DEFAULT v1,
                    serialNumber         CertificateSerialNumber,
                    signature            AlgorithmIdentifier,
                    issuer               Name,
                    validity             Validity,
                    subject              Name,
                    subjectPublicKeyInfo SubjectPublicKeyInfo,
                    issuerUniqueID  [1]  IMPLICIT UniqueIdentifier OPTIONAL,
                    subjectUniqueID [2]  IMPLICIT UniqueIdentifier OPTIONAL,
                                            -- If present, version MUST be v2 or v3
                    extensions      [3]  EXPLICIT Extensions OPTIONAL
                                            -- If present, version MUST be v3 }*/
            var sequence = reader.ReadSequence();

            // version         [0]  EXPLICIT Version DEFAULT v1,
            // Version  ::=  INTEGER  {  v1(0), v2(1), v3(2)  }
            if (!sequence.PeekTag().HasSameClassAndValue(new Asn1Tag(TagClass.ContextSpecific, 0)))
            {
                throw new CryptographicException();
            }
            // workaround for reading interger with a context-specific tag
            var versionData = sequence.ReadEncodedValue().Slice(2); // strip outter tag: context-specific#0
            var versionReader = new AsnReader(versionData, AsnEncodingRules.BER);
            if (!versionReader.TryReadInt32(out var version))
            {
                versionReader.ThrowIfNotEmpty();
                sequence.ThrowIfNotEmpty();
            }

            // serialNumber         CertificateSerialNumber,
            // CertificateSerialNumber  ::=  INTEGER
            var serialNumber = sequence.ReadInteger();

            //  signature            AlgorithmIdentifier,
            var signature = AlgorithmIdentifier.Decode(sequence);

            // issuer               Name,
            var issuer = DistinguishedName.Decode(sequence);

            // validity             Validity,
            var validity = sequence.ReadEncodedValue();

            // subject              Name,
            var subject = DistinguishedName.Decode(sequence);

            // subjectPublicKeyInfo SubjectPublicKeyInfo,
            // issuerUniqueID  [1]  IMPLICIT UniqueIdentifier OPTIONAL,
            // subjectUniqueID [2]  IMPLICIT UniqueIdentifier OPTIONAL,
            //                      -- If present, version MUST be v2 or v3
            // extensions      [3]  EXPLICIT Extensions OPTIONAL
            //                      -- If present, version MUST be v3 }
            var subjectPublicKeyInfo = sequence.ReadEncodedValue();
            var issuerUniqueID = sequence.ReadOptional(1, (x, t) => x.ReadEncodedValue());
            var subjectUniqueID = sequence.ReadOptional(2, (x, t) => x.ReadEncodedValue());

            // extensions      [3]  EXPLICIT Extensions OPTIONAL
            //                      -- If present, version MUST be v3 }
            // Extensions  ::=  SEQUENCE SIZE (1..MAX) OF Extension
            var extensions = sequence.ReadOptional(3, (r, t) => r.ReadSequence(t), r => r.ReadSequenceOf(x => TBSCertificateExtension.Decode(x)));

            if (sequence.HasData) sequence.PeekTag();
            sequence.ThrowIfNotEmpty();
            return new()
            {
                Version = version,
                SerialNumber = serialNumber,
                Signature = signature,
                Issuer = issuer,
                Validity = validity,
                Subject = subject,
                SubjectPublicKeyInfo = subjectPublicKeyInfo,
                IssuerUniqueID = issuerUniqueID,
                SubjectUniqueID = subjectUniqueID,
                Extensions = extensions,
            };
        }
    }
}
