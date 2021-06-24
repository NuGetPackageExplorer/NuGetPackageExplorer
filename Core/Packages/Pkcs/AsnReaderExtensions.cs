using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.Packages.Pkcs
{
    internal static class AsnReaderExtensions
    {
        // Set vs Sequence: set = unordered, sequence = ordered
        // Set,Sequence vs SetOf,SequenceOf: X = may be different type, XOf = of the same type

        internal static ReadOnlyMemory<byte> ReadPrimitiveOrNotOctetString(this AsnReader reader, Asn1Tag? expectedTag = null)
        {
            return reader.TryReadPrimitiveOctetString(out var value, expectedTag)
                ? value
                : reader.ReadOctetString(expectedTag);
        }

        internal static T[] ReadSetOf<T>(this AsnReader reader, Func<AsnReader, T> readElement)
        {
            IEnumerable<T> ReadSetOfCore()
            {
                var set = reader.ReadSetOf();
                while (set.HasData)
                {
                    yield return readElement(set);
                }
                set.ThrowIfNotEmpty();
            }

            // ToArray to ensure immediate materialization. If done lazily, it will break integrity validation, as the data wont be consumed.
            return ReadSetOfCore().ToArray();
        }
        internal static T[] ReadSequenceOf<T>(this AsnReader reader, Func<AsnReader, T> readElement)
        {
            IEnumerable<T> ReadSequenceOfCore()
            {
                var set = reader.ReadSequence();
                while (set.HasData)
                {
                    yield return readElement(set);
                }
                set.ThrowIfNotEmpty();
            }

            // ToArray to ensure immediate materialization. If done lazily, it will break integrity validation, as the data wont be consumed.
            return ReadSequenceOfCore().ToArray();
        }

        internal delegate bool TryReadDelegate<T>(out T value, Asn1Tag? expectedTag = null);
        internal static T ReadOrThrow<T>(this AsnReader reader, Asn1Tag? tag, TryReadDelegate<T> tryRead)
        {
            return tryRead(out var value, tag)
                ? value
                : throw new AsnContentException();
        }

        internal static T? ReadOptionalSequence<T>(this AsnReader reader, int? context, Func<AsnReader, T> read) => reader.ReadOptional(context, (r, tag) => r.ReadSequence(tag), read);
        internal static T[]? ReadOptionalSequenceOf<T>(this AsnReader reader, int? context, Func<AsnReader, T> readElement)
        {
            // ToArray to ensure immediate materialization. If done lazily, it will break integrity validation, as the data wont be consumed.
            return reader.ReadOptional(context, (r, tag) => r.ReadSequence(tag), sequence => ReadCore(sequence).ToArray());

            IEnumerable<T> ReadCore(AsnReader sequence)
            {
                while (sequence.HasData)
                {
                    yield return readElement(sequence);
                }
                sequence.ThrowIfNotEmpty();
            }
        }
        internal static T[]? ReadOptionalSetOf<T>(this AsnReader reader, int? context, Func<AsnReader, T> readElement)
        {
            // ToArray to ensure immediate materialization. If done lazily, it will break integrity validation, as the data wont be consumed.
            return reader.ReadOptional(context, (r, tag) => r.ReadSetOf(tag), set => ReadCore(set).ToArray());

            IEnumerable<T> ReadCore(AsnReader set)
            {
                while (set.HasData)
                {
                    yield return readElement(set);
                }
                set.ThrowIfNotEmpty();
            }
        }
        internal static T? ReadOptional<T>(this AsnReader reader, int? context, Func<AsnReader, Asn1Tag, AsnReader> readOptional, Func<AsnReader, T> read)
        {
            var contextTag = context.HasValue
                ? new Asn1Tag(TagClass.ContextSpecific, context.Value)
                : default(Asn1Tag?);
            if (reader.HasData && (!contextTag.HasValue || reader.PeekTag().HasSameClassAndValue(contextTag.Value)))
            {
                var optional = readOptional(reader, contextTag ?? reader.PeekTag());
                var result = read(optional);

                if (reader != optional) optional.ThrowIfNotEmpty();
                return result;
            }

            return default;
        }
        internal static T? ReadOptional<T>(this AsnReader reader, int context, Func<AsnReader, Asn1Tag, T> readOptional)
        {
            return reader.ReadOptional(new Asn1Tag(TagClass.ContextSpecific, context), readOptional);
        }
        internal static T? ReadOptional<T>(this AsnReader reader, Asn1Tag? tag, Func<AsnReader, Asn1Tag, T> readOptional)
        {
            if (reader.HasData && tag?.HasSameClassAndValue(reader.PeekTag()) != false)
            {
                var result = readOptional(reader, tag ?? reader.PeekTag());

                return result;
            }

            return default;
        }
    }
}
