using System.Reflection;

namespace NuGetPe
{
    public static class AssemblyExtensions
    {
        public static AssemblyName GetNameSafe(this Assembly assembly)
        {
            return new AssemblyName(assembly.FullName);
        }
    }
}