using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGetPackageExplorer.Types
{
    /// <summary>
    /// The replacement tokens allowed in a nuspec file.
    /// </summary>
    /// <remarks>
    /// http://docs.nuget.org/create/nuspec-reference
    /// </remarks>
    public static class ReplacementTokens
    {
        /// <summary>
        /// The Assembly name
        /// </summary>
        public const string Id = "$id$";

        /// <summary>
        /// The assembly version as specified in the assembly’s <see cref="AssemblyVersionAttribute"/>. 
        /// If the assembly’s <see cref="AssemblyInformationalVersionAttribute"/> is specified, that one is used instead.
        /// </summary>
        public const string Version = "$version$";

        /// <summary>
        /// The company as specified in the <see cref="AssemblyCompanyAttribute "/>.
        /// </summary>
        public const string Author = "$author$";

        /// <summary>
        /// 	The description as specified in the <see cref="AssemblyDescriptionAttribute"/>.
        /// </summary>
        public const string Description = "$description$";

        /// <summary>
        /// This element contains a set of &lt;reference&gt; elements, each of which specifies an assembly that will be referenced by the project. 
        /// The existence of this element overrides the convention of pulling everything in the lib folder. 
        /// </summary>
        public const string References = "$references$";

        /// <summary>
        /// All replacement tokens as a set.
        /// </summary>
        public static HashSet<string> AllReplacementTokens = new HashSet<string> { Id, Version, Author, Description, References };


    }
}
