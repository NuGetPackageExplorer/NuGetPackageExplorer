using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NuGetPackageExplorer.Types;
using System.ComponentModel.Composition;
using NuGet;

namespace $rootnamespace$ 
{

    [Export(typeof(IPackageRule))]
    internal class MyCustomPackageRule : IPackageRule 
    {

        public string Name 
        {
            get 
            {
                return "My Custom Rule";
            }
        }

        public IEnumerable<PackageIssue> Check(IPackage package) 
        {
            throw new NotImplementedException();
        }
    }
}
