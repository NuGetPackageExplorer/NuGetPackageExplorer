using System.Collections.Generic;
using NuGet.Versioning;

namespace NuGetPe
{
    public class DeprecationInfo
    {
        public string? Message { get; set; }
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public IEnumerable<string> Reasons { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public AlternatePackageInfo? AlternatePackageInfo { get; set; }
    }

    public class AlternatePackageInfo
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public string Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        public VersionRange Range { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
