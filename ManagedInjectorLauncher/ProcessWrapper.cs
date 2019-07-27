namespace ManagedInjectorLauncher
{
    using System;
    using System.Diagnostics;
    using Snoop;

    public class ProcessWrapper
    {
        public ProcessWrapper(Process process)
        {
            this.Process = process;
            this.Id = process.Id;
            this.Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, process.Id);

            this.Bitness = NativeMethods.IsProcess64Bit(this.Process)
                               ? "x64"
                               : "x86";

            this.SupportedFrameworkName = GetTargetFramework(process);
        }

        public Process Process { get; }

        public int Id { get; }

        public NativeMethods.ProcessHandle Handle { get; }

        public string Bitness { get; }

        public string SupportedFrameworkName { get; }

        public static ProcessWrapper FromProcessId(int id)
        {
            return new ProcessWrapper(Process.GetProcessById(id));
        }

        public static ProcessWrapper FromWindowHandle(IntPtr handle)
        {
            return new ProcessWrapper(GetProcessFromWindowHandle(handle));
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

        private static string GetTargetFramework(Process process)
        {
            var modules = NativeMethods.GetModules(process);

            foreach (var module in modules)
            {
                if (module.szModule.IndexOf("wpfgfx_cor3", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "netcoreapp3.0";
                }
            }

            return "net40";
        }
    }
}