using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace CodeExecutor
{
    public class AppDomainWorker : MarshalByRefObject
    {
        public bool? CheckIsNpeMetroInstalled(string assemblyPath)
        {
            object ret = ExecuteCode(assemblyPath, "Windows8Shim.NpeAppChecker", "CheckIsNpeMetroInstalled");
            return (bool?)ret;
        }

        private object ExecuteCode(string assemblyPath, string typeName, string methodName)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            Type type = assembly.GetType(typeName);
            if (type != null)
            {
                var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (methodInfo != null)
                {
                    try
                    {
                        return methodInfo.Invoke(null, new object[0]);
                        //logger.Log(MessageLevel.Info, "Method executed succcessfully.");
                    }
                    catch (Exception)
                    {
                        //logger.Log(MessageLevel.Error, exception.GetBaseException().Message);
                    }
                }
            }

            return null;
        }

        public AssemblyMetaData GetAssemblyMetadata(string assemblyPath)
        {
            var data = new AssemblyMetaData();

            var assemblyName = AssemblyName.GetAssemblyName(assemblyPath);
            if (assemblyName == null)
            {
                return data;
            }

            // For WinRT component, we can only read Full Name. 
            if (assemblyName.ContentType == AssemblyContentType.WindowsRuntime)
            {
                data.SetFullName(assemblyName.FullName);
                return data;
            }

            var assembly = Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            if (assembly != null)
            {

                data.SetFullName(assembly.FullName);

                try
                {
                    foreach (var attribute in assembly.GetCustomAttributesData())
                    {
                        if (attribute.ConstructorArguments.Count != 1 ||
                            attribute.ConstructorArguments[0].ArgumentType != typeof(string))
                        {
                            continue;
                        }

                        string typeName = attribute.Constructor.DeclaringType.Name;
                        if (typeName == "InternalsVisibleToAttribute")
                        {
                            continue;
                        }

                        string key = typeName.EndsWith("Attribute", StringComparison.OrdinalIgnoreCase)
                            ? typeName.Substring(0, typeName.Length - 9)
                            : typeName;

                        string value = attribute.ConstructorArguments[0].Value.ToString();

                        if (!String.IsNullOrEmpty(value))
                        {
                            data[key] = value;
                        }
                    }
                }
                catch (Exception)
                {
                    // if an exception occurs when loading custom attributes, just ignore
                }

                try
                {
                    data[AssemblyMetaData.ReferencedAssembliesKey] = string.Join(
                        Environment.NewLine,
                        assembly.GetReferencedAssemblies().OrderBy(assName => assName.Name)
                    );
                }
                catch
                {
                    // Ignore if unable to obtain referenced assemblies
                }
            }

            return data;
        }

        /// <summary>
        ///  Setup a hook for robust lookup of the required dependencies.
        /// </summary>
        public void SetupAssemblyLoadHook(AssemblyName[] knownAssemblyNames)
        {
            if (knownAssemblyNames == null) throw new ArgumentNullException(nameof(knownAssemblyNames));

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += (sender, resolveArgs) =>
            {
                var assemblyName = new AssemblyName(resolveArgs.Name);

                bool IsNameMatching(AssemblyName other) =>
                    assemblyName.Name.Equals(other.Name, StringComparison.Ordinal);

                Assembly result = null;

                // Try load the assembly using the name without adjustments.
                try
                {
                    result = Assembly.ReflectionOnlyLoad(assemblyName.FullName);
                }
                catch
                {
                    // Ignore if cannot load by direct reference.
                }
                if (result != null)
                {
                    return result;
                }

                // Find the already loaded assembly of a different version.
                // It doesn't matter for us as we read attributes only.
                result = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                    .FirstOrDefault(a => IsNameMatching(a.GetName()));
                if (result != null)
                {
                    return result;
                }

                // Try to find this assembly in other known version. If found - try to load in that version.
                try
                {
                    var knownAssemblyName = knownAssemblyNames.FirstOrDefault(IsNameMatching);
                    if (knownAssemblyName != null)
                    {
                        result = Assembly.ReflectionOnlyLoad(knownAssemblyName.FullName);
                    }
                }
                catch
                {
                    // Ignore if cannot load assembly.
                }
                if (result != null)
                {
                    return result;
                }

                // Usually we look for the system assemblies, however of different .NET Framework versions.
                // Try to adjust the required version using the current mscorlib's assembly.
                // In most cases that helps to find the matching assembly in current .NET Framework.
                try
                {
                    var mscorlib = knownAssemblyNames.FirstOrDefault(x =>
                        x.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase));
                    if (mscorlib != null)
                    {
                        var nameWithAdjustedVersion = (AssemblyName) assemblyName.Clone();
                        nameWithAdjustedVersion.Version = mscorlib.Version;

                        result = Assembly.ReflectionOnlyLoad(nameWithAdjustedVersion.FullName);
                    }
                }
                catch
                {
                    // Ignore if cannot load adjusted version.
                }

                return result;
            };
        }
    }
}