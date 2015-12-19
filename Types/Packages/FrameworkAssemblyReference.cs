using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet
{
    public class FrameworkAssemblyReference : IFrameworkTargetable
    {
        public FrameworkAssemblyReference(string assemblyName)
            : this(assemblyName, Enumerable.Empty<FrameworkName>())
        {
        }

        public FrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks,
                                          string displayValue = null)
        {
            if (String.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentException("Argument is null or empty.", "assemblyName");
            }

            if (supportedFrameworks == null)
            {
                throw new ArgumentNullException("supportedFrameworks");
            }

            DisplayString = displayValue ?? String.Join("; ", supportedFrameworks);
            AssemblyName = assemblyName;
            SupportedFrameworks = supportedFrameworks;
        }

        public string DisplayString { get; private set; }
        public string AssemblyName { get; private set; }

        #region IFrameworkTargetable Members

        public IEnumerable<FrameworkName> SupportedFrameworks { get; private set; }

        #endregion

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