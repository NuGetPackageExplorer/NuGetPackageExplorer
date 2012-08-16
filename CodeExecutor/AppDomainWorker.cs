using System;
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
    }
}