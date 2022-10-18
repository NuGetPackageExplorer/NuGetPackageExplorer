using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

// docs reference: https://tools.ietf.org/html/rfc5280
// impl reference: n/a

namespace NuGetPe.Packages.Pkcs
{
    public class DistinguishedName
    {
        private class Oids
        {
            // https://oidref.com/2.5.4
            public const string CommonName = "2.5.4.3";
            public const string SerialNumber = "2.5.4.5";
            public const string CountryName = "2.5.4.6";
            public const string LocalityName = "2.5.4.7";
            public const string StateOrProvinceName = "2.5.4.8";
            public const string OrganizationName = "2.5.4.10";
        }
        private static readonly IReadOnlyDictionary<string, string> SubjectLabelMappings = new Dictionary<string, string>
        {
            [Oids.CommonName] = "CN",
            [Oids.OrganizationName] = "O",
            [Oids.LocalityName] = "L",
            [Oids.StateOrProvinceName] = "S",
            [Oids.CountryName] = "C",
            [Oids.SerialNumber] = "SERIALNUMBER",
        };

        public IReadOnlyCollection<(string Type, string Value)> ValueCollection { get; private set; } = Array.Empty<(string Type, string Value)>();

        private string? this[string oid] => ValueCollection.CastAsNullable().FirstOrDefault(x => x?.Type == oid)?.Value;
        public string? CommonName => this[Oids.CommonName];
        public string? SerialNumber => this[Oids.SerialNumber];
        public string? CountryName => this[Oids.CountryName];
        public string? LocalityName => this[Oids.LocalityName];
        public string? StateOrProvinceName => this[Oids.StateOrProvinceName];
        public string? OrganizationName => this[Oids.OrganizationName];

        public static DistinguishedName Decode(AsnReader reader)
        {
            // https://tools.ietf.org/html/rfc5280
            /*  Name ::= CHOICE { -- only one possibility for now --
                    rdnSequence  RDNSequence }
                RDNSequence ::= SEQUENCE OF RelativeDistinguishedName
                RelativeDistinguishedName ::=
                    SET SIZE (1..MAX) OF AttributeTypeAndValue
                AttributeTypeAndValue ::= SEQUENCE {
                    type     AttributeType,
                    value    AttributeValue }
                AttributeType ::= OBJECT IDENTIFIER
                AttributeValue ::= ANY -- DEFINED BY AttributeType
                DirectoryString ::= CHOICE {
                    teletexString           TeletexString (SIZE (1..MAX)),
                    printableString         PrintableString (SIZE (1..MAX)),
                    universalString         UniversalString (SIZE (1..MAX)),
                    utf8String              UTF8String (SIZE (1..MAX)),
                    bmpString               BMPString (SIZE (1..MAX)) } */
            var RDNs = reader.ReadSequenceOf(sequence =>
                sequence.ReadSetOf(set =>
                {
                    var sequence = set.ReadSequence();
                    var type = sequence.ReadObjectIdentifier();
                    var value = sequence.PeekTag() is { } tag2 && tag2.TagClass == TagClass.Universal
                        ? sequence.ReadCharacterString((UniversalTagNumber)tag2.TagValue)
                        : throw new CryptographicException();


                    sequence.ThrowIfNotEmpty();
                    return (type, value);
                })
            );

            return new()
            {
                ValueCollection = RDNs.SelectMany(x => x).ToArray()
            };
        }

        public override string ToString()
        {
            // Windows Certificate Manager displays the attributes in LDAP order, which is the reverse order to which they are written.
            // CN=Microsoft Code Signing PCA 2011, O=Microsoft Corporation, L=Redmond, S=Washington, C=US

            return string.Join(", ", ValueCollection
                .Reverse()
                .Select(x => $"{(SubjectLabelMappings.TryGetValue(x.Type, out var alias) ? alias : x.Type)}={x.Value}")
            );
        }

        public static bool operator ==(DistinguishedName? a, DistinguishedName? b) => a is null ? b is null : a.Equals(b);
        public static bool operator !=(DistinguishedName? a, DistinguishedName? b) => !(a == b);
        
        public override int GetHashCode() => ValueCollection.GetHashCode();
        public override bool Equals(object? obj)
        {
            if (obj is DistinguishedName that)
            {
                if ((this.ValueCollection?.Count ?? 0) == (that.ValueCollection?.Count ?? 0)) return true;
                var empty = Array.Empty<(string Type, string Value)>();

                return (this.ValueCollection ?? empty).SequenceEqual(that.ValueCollection ?? empty);
            }

            return false;
        }
    }
}
