using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;

namespace NuGet {
    public class FrameworkAssemblyReference : IFrameworkTargetable {
        public FrameworkAssemblyReference(string assemblyName)
            : this(assemblyName, Enumerable.Empty<FrameworkName>()) {
        }

        public FrameworkAssemblyReference(string assemblyName, IEnumerable<FrameworkName> supportedFrameworks) {
            if (String.IsNullOrEmpty(assemblyName)) {
                throw new ArgumentException("Argument is null or empty.", "assemblyName");
            }

            if (supportedFrameworks == null) {
                throw new ArgumentNullException("supportedFrameworks");
            }

            AssemblyName = assemblyName;
            SupportedFrameworks = supportedFrameworks;
        }

        public string AssemblyName { get; private set; }
        public IEnumerable<FrameworkName> SupportedFrameworks { get; private set; }

        public override string ToString() {
            return String.Format(CultureInfo.CurrentCulture, "{0} ({1})", AssemblyName,
                                 String.Join("; ", SupportedFrameworks));
        }
    }
}
