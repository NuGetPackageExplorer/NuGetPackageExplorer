using System;

namespace NuGet {
    public class AssemblyReference {
        public string File { get; private set; }

        public AssemblyReference(string file) {
            if (file == null) {
                throw new ArgumentNullException("file");
            }

            File = file;
        }
    }
}
