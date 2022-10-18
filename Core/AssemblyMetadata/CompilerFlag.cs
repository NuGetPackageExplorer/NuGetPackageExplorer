using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGetPe.AssemblyMetadata
{
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public struct CompilerFlag : IEquatable<CompilerFlag>
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is CompilerFlag flag && Equals(flag);
        }

        public bool Equals(CompilerFlag other)
        {
            return Key == other.Key &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Value);
        }

        public static bool operator ==(CompilerFlag left, CompilerFlag right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CompilerFlag left, CompilerFlag right)
        {
            return !(left == right);
        }
    }
}
