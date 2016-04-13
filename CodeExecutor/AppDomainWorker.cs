using System;
using System.Collections.Generic;
using System.Reflection;

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

            var assembly = Assembly.Load(assemblyName);
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
            }

            return data;
        }
    }
}