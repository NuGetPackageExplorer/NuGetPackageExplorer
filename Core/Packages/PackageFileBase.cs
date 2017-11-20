using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using NuGet;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NuGetPe
{
    public abstract class PackageFileBase : IPackageFile
    {
        private readonly NuGetFramework _targetFramework;

        protected PackageFileBase(string path)
        {
            Path = path;
            
            string effectivePath;
            FrameworkNameUtility.ParseFrameworkNameFromFilePath(path, out effectivePath);
            _targetFramework = NuGetFramework.Parse(effectivePath);
            EffectivePath = effectivePath;
        }

        public string Path
        {
            get;
            private set;
        }

        public virtual string OriginalPath
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

        public NuGetFramework TargetFramework
        {
            get
            {
                return _targetFramework;
            }
        }

        public IEnumerable<NuGetFramework> SupportedFrameworks
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
    }
}
