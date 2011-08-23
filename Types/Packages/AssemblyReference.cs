using System;

namespace NuGet {
    public class AssemblyReference : IEquatable<AssemblyReference> {
        public string File { get; private set; }

        public AssemblyReference(string file) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            File = file;
        }

        public bool Equals(AssemblyReference other) {
            return File.Equals(other.File, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode() {
            return File.GetHashCode();
        }
    }
}
