// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace ManagedInjectorLauncher
{
    using System;
    using System.Diagnostics;
    using System.Linq;
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

            var windowHandle = (IntPtr)long.Parse(args[0]);
            var assemblyName = args[1];
            var className = args[2];
            var methodName = args[3];
            var settingsFile = args[4];

            var injectorData = new InjectorData
                               {
                                   AssemblyName = assemblyName,
                                   ClassName = className,
                                   MethodName = methodName,
                                   SettingsFile = settingsFile
                               };

            try
            {
                Injector.InjectIntoProcess(windowHandle, injectorData);
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception.ToString());
                return 1;
            }

            //check to see that it was injected, and if not, retry with the main window handle.
            var process = GetProcessFromWindowHandle(windowHandle);
            if (process != null
                && CheckInjectedStatus(process) == false
                && process.MainWindowHandle != windowHandle)
            {
                Injector.LogMessage("Could not inject with current handle... retrying with MainWindowHandle", true);

                try
                {
                    Injector.InjectIntoProcess(process.MainWindowHandle, injectorData);
                }
                catch (Exception exception)
                {
                    Trace.WriteLine(exception.ToString());
                    return 1;
                }

                if (CheckInjectedStatus(process) == false)
                {
                    return 1;
                }
            }
            else
            {
                return 1;
            }

            return 0;
        }

        private static Process GetProcessFromWindowHandle(IntPtr windowHandle)
        {
            NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

            if (processId == 0)
            {
                Injector.LogMessage($"Could not get process for window handle {windowHandle}", true);
                return null;
            }

            try
            {
                var process = Process.GetProcessById(processId);
                return process;
            }
            catch (Exception e)
            {
                Injector.LogMessage($"Could not get process for PID = {processId}.", true);
                Injector.LogMessage(e.ToString(), true);
            }

            return null;
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