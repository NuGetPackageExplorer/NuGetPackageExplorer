using System;

namespace NuGet {
    public interface IVersionSpec {
        Version MinVersion { get; }
        bool IsMinInclusive { get; }
        Version MaxVersion { get; }
        bool IsMaxInclusive { get; }
    }
}
