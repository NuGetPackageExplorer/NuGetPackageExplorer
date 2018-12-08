using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NuGetPe.AssemblyMetadata
{
    [DebuggerDisplay("{Name}")]
    public class AssemblyDebugSourceDocument
    {
        private static readonly Guid CSharp = new Guid("3f5162f8-07c6-11d3-9053-00c04fa302a1");
        private static readonly Guid VisualBasic = new Guid("3a12d0b8-c26c-11d0-b442-00a0244a1dd2");
        private static readonly Guid FSharp = new Guid("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3");

        private static readonly Guid Md5 = new Guid("406ea660-64cf-4c82-b6f0-42d48172a799");
        private static readonly Guid Sha1 = new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460");
        private static readonly Guid Sha256 = new Guid("8829d00f-11b8-4213-878b-770e8597ac16");

        internal AssemblyDebugSourceDocument(string name, byte[] hash, Guid language, Guid hashAlgorithm)
        {
            Name = name;
            Hash = hash;
            Language = LanguageFromGuid(language);
            HashAlgorithm = HashAlgorithmNameFromGuid(hashAlgorithm);
        }

        public string Name { get; }
        public byte[] Hash { get; }
        public SymbolLanguage Language { get; }
        public HashAlgorithmName HashAlgorithm { get; }

        private SymbolLanguage LanguageFromGuid(Guid guid)
        {
            if (guid == CSharp) return SymbolLanguage.CSharp;
            if (guid == VisualBasic) return SymbolLanguage.VisualBasic;
            if (guid == FSharp) return SymbolLanguage.FSharp;

            throw new ArgumentOutOfRangeException(nameof(guid));
        }


        public HashAlgorithmName HashAlgorithmNameFromGuid(Guid guid)
        {
            if (guid == Md5) return HashAlgorithmName.MD5;
            if (guid == Sha1) return HashAlgorithmName.SHA1;
            if (guid == Sha256) return HashAlgorithmName.SHA256;

            throw new ArgumentOutOfRangeException(nameof(guid));
        }
    }

    public enum SymbolLanguage
    {
        [Description("C#")]
        CSharp,
        [Description("VB")]
        VisualBasic,
        [Description("F#")]
        FSharp
    }
}
