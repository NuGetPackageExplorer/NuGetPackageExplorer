using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NuGet.Frameworks;
using Packaging = NuGet.Packaging;

namespace NuGetPe
{
    public class FrameworkAssemblyReference : Packaging.FrameworkAssemblyReference
    {
        public FrameworkAssemblyReference(string assemblyName)
            : base(assemblyName, Enumerable.Empty<NuGetFramework>())
        {
        }

        public FrameworkAssemblyReference(string assemblyName, IEnumerable<NuGetFramework> supportedFrameworks,
                                          string displayValue = null) : base(assemblyName, supportedFrameworks)
        {

            DisplayString = displayValue ?? String.Join("; ", supportedFrameworks);
        }
        public string DisplayString { get; private set; }

        public override string ToString()
        {
            if (SupportedFrameworks.Any())
            {
                return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", AssemblyName,
                                     String.Join("; ", SupportedFrameworks));
            }
            else
            {
                return AssemblyName;
            }
        }
    }
}