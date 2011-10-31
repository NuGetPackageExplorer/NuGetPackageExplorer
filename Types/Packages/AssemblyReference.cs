using System;

namespace NuGet
{
    public class AssemblyReference : IEquatable<AssemblyReference>
    {
        public AssemblyReference(string file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            File = file;
        }

        public string File { get; private set; }

        #region IEquatable<AssemblyReference> Members

        public bool Equals(AssemblyReference other)
        {
            return File.Equals(other.File, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        public override int GetHashCode()
        {
            return File.GetHashCode();
        }
    }
}