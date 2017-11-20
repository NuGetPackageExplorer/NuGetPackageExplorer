using NuGet.Frameworks;
using NuGet.Packaging.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGetPe
{
    public class PackageDependencySet : IFrameworkTargetable
    {
        private readonly NuGetFramework _targetFramework;
        private readonly ReadOnlyCollection<PackageDependency> _dependencies;

        public PackageDependencySet(NuGetFramework targetFramework, IEnumerable<PackageDependency> dependencies)
        {
            if (dependencies == null)
            {
                throw new ArgumentNullException("dependencies");
            }

            _targetFramework = targetFramework;
            _dependencies = new ReadOnlyCollection<PackageDependency>(dependencies.ToArray());
        }

        public NuGetFramework TargetFramework
        {
            get
            {
                return _targetFramework;
            }
        }

        public ICollection<PackageDependency> Dependencies
        {
            get
            {
                return _dependencies;
            }
        }

        public IEnumerable<NuGetFramework> SupportedFrameworks
        {
            get
            {
                if (TargetFramework == null)
                {
                    yield break;
                }

                yield return TargetFramework;
            }
        }
    }
}