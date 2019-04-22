// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace ManagedInjectorLauncher
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Snoop;

    public static class Program
    {
        public static void Main(string[] args)
        {
            InjectorHelper.LogMessage("Starting the injection process...", false);

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

            InjectorHelper.Launch(windowHandle, injectorData);

            //check to see that it was injected, and if not, retry with the main window handle.
            var process = GetProcessFromWindowHandle(windowHandle);
            if (process != null
                && CheckInjectedStatus(process) == false
                && process.MainWindowHandle != windowHandle)
            {
                InjectorHelper.LogMessage("Could not inject with current handle... retrying with MainWindowHandle", true);
                InjectorHelper.Launch(process.MainWindowHandle, injectorData);
                CheckInjectedStatus(process);
            }
        }

        private static Process GetProcessFromWindowHandle(IntPtr windowHandle)
        {
            GetWindowThreadProcessId(windowHandle, out var processId);
            if (processId == 0)
            {
                InjectorHelper.LogMessage($"could not get process for window handle {windowHandle}", true);
                return null;
            }

            try
            {
                var process = Process.GetProcessById(processId);
                return process;
            }
            catch (Exception e)
            {
                InjectorHelper.LogMessage($"could not get process for PID = {processId}.", true);
                InjectorHelper.LogMessage(e.ToString(), true);
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
                InjectorHelper.LogMessage(string.Format("Successfully injected Snoop for process {0} (PID = {1})", process.ProcessName, process.Id), true);
            }
            else
            {
                InjectorHelper.LogMessage(string.Format("Failed to inject for process {0} (PID = {1})", process.ProcessName, process.Id), true);
            }

            return containsFile;
        }

        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);
    }
}