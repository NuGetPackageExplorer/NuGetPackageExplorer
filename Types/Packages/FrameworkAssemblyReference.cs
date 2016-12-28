using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGetPe
{
    public class FrameworkAssemblyReference : NuGet.FrameworkAssemblyReference
    {
        public FrameworkAssemblyReference(string assemblyName)
            : base(assemblyName, Enumerable.Empty<FrameworkName>())
        {
        }

        public FrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks,
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