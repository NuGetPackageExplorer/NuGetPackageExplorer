using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NuGetPe
{
    public abstract class PackageFileBase : IPackageFile
    {
        protected PackageFileBase(string path)
        {
            Path = path;

            FrameworkNameUtility.ParseFrameworkNameFromFilePath(path, out var effectivePath);
            EffectivePath = effectivePath;

            try
            {
                TargetFramework = new FrameworkName(NuGetFramework.Parse(effectivePath).DotNetFrameworkName);
            }
            catch (ArgumentException) // could be an invalid framework/version
            {

            }
        }

        public string Path
        {
            get;
            private set;
        }

        public virtual string? OriginalPath
        {
            get
            {
                return null;
            }
        }

        public abstract Stream GetStream();

        public string EffectivePath
        {
            get;
            private set;
        }

        public FrameworkName? TargetFramework { get; }

        public IEnumerable<FrameworkName> SupportedFrameworks
        {
            get
            {
                if (TargetFramework != null)
                {
                    yield return TargetFramework;
                }
                yield break;
            }
        }

        public virtual DateTimeOffset LastWriteTime { get; } = DateTimeOffset.MinValue;
    }
}
