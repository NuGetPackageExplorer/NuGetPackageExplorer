using NuGet.Frameworks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGetPe
{
    public class PackageReferenceSet : IFrameworkTargetable
    {
        private readonly FrameworkName _targetFramework;
        private readonly ICollection<string> _references;

        public PackageReferenceSet(FrameworkName targetFramework, IEnumerable<string> references)
        {
            if (references == null)
            {
                throw new ArgumentNullException("references");
            }

            _targetFramework = targetFramework;
            _references = new ReadOnlyCollection<string>(references.ToList());
        }

        public ICollection<string> References
        {
            get
            {
                return _references;
            }
        }

        public FrameworkName TargetFramework
        {
            get { return _targetFramework; }
        }

        public IEnumerable<FrameworkName> SupportedFrameworks
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