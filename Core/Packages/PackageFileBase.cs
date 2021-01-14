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
            
            try
            {
                var nuf = FrameworkNameUtility.ParseNuGetFrameworkFromFilePath(path, out var effectivePath);

                EffectivePath = effectivePath;

                NuGetFramework = nuf;
                if(nuf != null)
                {
                    TargetFramework = new FrameworkName(NuGetFramework.DotNetFrameworkName);
                }                
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
        public NuGetFramework? NuGetFramework { get; }

        public IEnumerable<NuGetFramework> SupportedFrameworks
        {
            get
            {
                if (NuGetFramework != null)
                {
                    yield return NuGetFramework;
                }
                yield break;
            }
        }

        public virtual DateTimeOffset LastWriteTime { get; } = DateTimeOffset.MinValue;

        
    }
}
