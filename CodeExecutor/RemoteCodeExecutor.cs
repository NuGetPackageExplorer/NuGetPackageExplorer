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
                var fullPath = typeof(RemoteCodeExecutor).Assembly.Location;
                var directory = Path.GetDirectoryName(fullPath);
                var assemblyPath = Path.Combine(directory, "Windows8Shim.dll");

                if (!File.Exists(assemblyPath))
                {
                    return false;
                }

                bool? result = null;
                ExecuteRemotely(worker => result = worker.CheckIsNpeMetroInstalled(assemblyPath));

                return result ?? true;
            }
        }

        private static void ExecuteRemotely(Action<AppDomainWorker> workerAction)
        {
            var domain = AppDomain.CreateDomain("CodeInvoker");
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
