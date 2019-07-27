// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace ManagedInjectorLauncher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Snoop;

    public static class Program
    {
        public static int Main(string[] args)
        {
            Injector.LogMessage("Starting the injection process...", false);

            if (args.Any(x => x.Equals("-debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }

            var processId = int.Parse(args[0]);
            
            var processWrapper = ProcessWrapper.FromProcessId(processId);

            var assemblyNameOrFullPath = args[1];
            var className = args[2];
            var methodName = args[3];
            var settingsFile = args[4];

            var injectorData = new InjectorData
                               {
                                   FullAssemblyPath = GetAssemblyPath(processWrapper, assemblyNameOrFullPath),
                                   ClassName = className,
                                   MethodName = methodName,
                                   SettingsFile = settingsFile
                               };

            try
            {
                Injector.InjectIntoProcess(processWrapper, injectorData);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception.ToString());
                return 1;
            }

            if (CheckInjectedStatus(processWrapper.Process) == false)
            {
                return 1;
            }

            return 0;
        }

        private static string GetAssemblyPath(ProcessWrapper processWrapper, string assemblyNameOrFullPath)
        {
            if (File.Exists(assemblyNameOrFullPath))
            {
                return assemblyNameOrFullPath;
            }

            var thisAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(thisAssemblyDirectory, $"{assemblyNameOrFullPath}.{processWrapper.SupportedFrameworkName}.dll");
        }

        private static bool CheckInjectedStatus(Process process)
        {
            var containsFile = false;
            process.Refresh();

            foreach (ProcessModule module in process.Modules)
            {
                if (module.FileName.Contains("ManagedInjector"))
                {
                    containsFile = true;
                }
            }

            if (containsFile)
            {
                Injector.LogMessage($"Successfully injected Snoop for process {process.ProcessName} (PID = {process.Id})", true);
            }
            else
            {
                Injector.LogMessage($"Failed to inject for process {process.ProcessName} (PID = {process.Id})", true);
            }

            return containsFile;
        }
    }
}