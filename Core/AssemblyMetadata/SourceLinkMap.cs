// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

#if NETCOREAPP
using System.Diagnostics.CodeAnalysis;
#endif

namespace Microsoft.SourceLink.Tools
{
    /// <summary>
    /// Source Link URL map. Maps file paths matching Source Link patterns to URLs.
    /// </summary>
    internal readonly struct SourceLinkMap
    {
        private readonly ReadOnlyCollection<Entry> _entries;

        private SourceLinkMap(ReadOnlyCollection<Entry> mappings)
        {
            _entries = mappings;
        }

        public readonly struct Entry
        {
            public readonly FilePathPattern FilePath;
            public readonly UriPattern Uri;

            public Entry(FilePathPattern filePath, UriPattern uri)
            {
                FilePath = filePath;
                Uri = uri;
            }

            public void Deconstruct(out FilePathPattern filePath, out UriPattern uri)
            {
                filePath = FilePath;
                uri = Uri;
            }
        }

        public readonly struct FilePathPattern
        {
            public readonly string Path;
            public readonly bool IsPrefix;

            public FilePathPattern(string path, bool isPrefix)
            {
                Path = path;
                IsPrefix = isPrefix;
            }
        }

        public readonly struct UriPattern
        {
            public readonly string Prefix;
            public readonly string Suffix;

            public UriPattern(string prefix, string suffix)
            {
                Prefix = prefix;
                Suffix = suffix;
            }
        }

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Parses Source Link JSON string.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
        /// <exception cref="InvalidDataException">The JSON does not follow Source Link specification.</exception>
        /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON string.</exception>
        public static SourceLinkMap Parse(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var list = new List<Entry>();

            var root = JsonDocument.Parse(json, new JsonDocumentOptions() { AllowTrailingCommas = true }).RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException();
            }

            foreach (var rootEntry in root.EnumerateObject())
            {
                if (!rootEntry.NameEquals("documents"))
                {
                    // potential future extensibility
                    continue;
                }

                if (rootEntry.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException();
                }

                foreach (var documentsEntry in rootEntry.Value.EnumerateObject())
                {
                    if (documentsEntry.Value.ValueKind != JsonValueKind.String ||
                        !TryParseEntry(documentsEntry.Name, documentsEntry.Value.GetString()!, out var entry))
                    {
                        throw new InvalidDataException();
                    }

                    list.Add(entry);
                }
            }

            // Sort the map by decreasing file path length. This ensures that the most specific paths will checked before the least specific
            // and that absolute paths will be checked before a wildcard path with a matching base
            list.Sort((left, right) => -left.FilePath.Path.Length.CompareTo(right.FilePath.Path.Length));

            return new SourceLinkMap(new ReadOnlyCollection<Entry>(list));
        }

        private static bool TryParseEntry(string key, string value, out Entry entry)
        {
            entry = default;

            // VALIDATION RULES
            // 1. The only acceptable wildcard is one and only one '*', which if present will be replaced by a relative path
            // 2. If the filepath does not contain a *, the uri cannot contain a * and if the filepath contains a * the uri must contain a *
            // 3. If the filepath contains a *, it must be the final character
            // 4. If the uri contains a *, it may be anywhere in the uri
            if (key.Length == 0)
            {
                return false;
            }

            var filePathStar = key.IndexOf('*', StringComparison.Ordinal);
            if (filePathStar == key.Length - 1)
            {
                key = key.Substring(0, filePathStar);
            }
            else if (filePathStar >= 0)
            {
                return false;
            }

            string uriPrefix, uriSuffix;
            var uriStar = value.IndexOf('*', StringComparison.Ordinal);
            if (uriStar >= 0)
            {
                if (filePathStar < 0)
                {
                    return false;
                }

                uriPrefix = value.Substring(0, uriStar);
                uriSuffix = value[(uriStar + 1)..];

                if (uriSuffix.Contains('*', StringComparison.Ordinal))
                {
                    return false;
                }
            }
            else
            {
                uriPrefix = value;
                uriSuffix = "";
            }

            entry = new Entry(
                new FilePathPattern(key, isPrefix: filePathStar >= 0),
                new UriPattern(uriPrefix, uriSuffix));

            return true;
        }

        /// <summary>
        /// Maps specified <paramref name="path"/> to the corresponding URL.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public bool TryGetUri(
            string path,
#if NETCOREAPP
            [NotNullWhen(true)]
#endif
            out string? uri)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.Contains('*', StringComparison.Ordinal))
            {
                uri = null;
                return false;
            }

            // Note: the mapping function is case-insensitive.

            foreach (var (file, mappedUri) in _entries)
            {
                if (file.IsPrefix)
                {
                    if (path.StartsWith(file.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        var escapedPath = string.Join("/", path[file.Path.Length..].Split(new[] { '/', '\\' }).Select(Uri.EscapeDataString));
                        uri = mappedUri.Prefix + escapedPath + mappedUri.Suffix;
                        return true;
                    }
                }
                else if (string.Equals(path, file.Path, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Assert(mappedUri.Suffix.Length == 0);
                    uri = mappedUri.Prefix;
                    return true;
                }
            }

            uri = null;
            return false;
        } 
    }
}
