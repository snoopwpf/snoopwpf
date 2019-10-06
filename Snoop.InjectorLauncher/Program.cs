// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.InjectorLauncher
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
                Injector.LogMessage($"Failed to inject Snoop into process {processWrapper.Process.ProcessName} (PID = {processWrapper.Process.Id})", true);
                Injector.LogMessage(exception.ToString(), true);
                return 1;
            }

            Injector.LogMessage($"Successfully injected Snoop into process {processWrapper.Process.ProcessName} (PID = {processWrapper.Process.Id})", true);

            return 0;
        }

        private static string GetAssemblyPath(ProcessWrapper processWrapper, string assemblyNameOrFullPath)
        {
            if (File.Exists(assemblyNameOrFullPath))
            {
                return assemblyNameOrFullPath;
            }

            var thisAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(thisAssemblyDirectory, processWrapper.SupportedFrameworkName, $"{assemblyNameOrFullPath}.{processWrapper.SupportedFrameworkName}.dll");
        }
    }
}