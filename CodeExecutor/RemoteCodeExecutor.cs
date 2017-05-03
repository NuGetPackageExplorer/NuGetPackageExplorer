using System;
using System.Collections.Generic;
using System.IO;

namespace CodeExecutor
{
    public static class RemoteCodeExecutor
    {
        public static bool IsNpeMetroInstalled
        {
            get
            {
                string fullPath = typeof(RemoteCodeExecutor).Assembly.Location;
                string directory = Path.GetDirectoryName(fullPath);
                string assemblyPath = Path.Combine(directory, "Windows8Shim.dll");

                if (!File.Exists(assemblyPath))
                {
                    return false;
                }

                bool? result = null;
                ExecuteRemotely(worker => result = worker.CheckIsNpeMetroInstalled(assemblyPath));

                return result ?? true;
            }
        }

        public static AssemblyMetaData GetAssemblyMetadata(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                return null;
            }

            AssemblyMetaData result = null;
            ExecuteRemotely(worker => result = worker.GetAssemblyMetadata(assemblyPath));
            return result;
        }

        private static void ExecuteRemotely(Action<AppDomainWorker> workerAction)
        {
            AppDomain domain = AppDomain.CreateDomain("CodeInvoker");
            try
            {
                // load CodeExecutor.dll into the other app domain
                domain.Load(typeof(RemoteCodeExecutor).Assembly.GetName());

                var worker = (AppDomainWorker)domain.CreateInstanceFromAndUnwrap(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CodeExecutor.dll"), "CodeExecutor.AppDomainWorker");

                workerAction(worker);
            }
            finally
            {
                AppDomain.Unload(domain);
            }
        }
    }
}
