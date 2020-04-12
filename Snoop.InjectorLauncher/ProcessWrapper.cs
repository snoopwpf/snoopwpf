namespace Snoop.InjectorLauncher
{
    using System;
    using System.Diagnostics;
    using Snoop.Infrastructure;

    public class ProcessWrapper
    {
        public ProcessWrapper(Process process, IntPtr windowHandle)
        {
            this.Process = process;
            this.Id = process.Id;
            this.Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, process.Id);
            this.WindowHandle = windowHandle;

            this.Bitness = GetBitnessAsString(this.Process);

            this.SupportedFrameworkName = GetTargetFramework(process);
        }

        public static string GetBitnessAsString(Process process)
        {
            return NativeMethods.IsProcess64Bit(process)
                ? "x64"
                : "x86";
        }

        public Process Process { get; }

        public int Id { get; }

        public NativeMethods.ProcessHandle Handle { get; }

        public IntPtr WindowHandle { get; set; }

        public string Bitness { get; }

        public string SupportedFrameworkName { get; }

        public static ProcessWrapper From(int processId, IntPtr windowHandle)
        {
            return new ProcessWrapper(Process.GetProcessById(processId), windowHandle);
        }

        public static ProcessWrapper FromWindowHandle(IntPtr handle)
        {
            return new ProcessWrapper(GetProcessFromWindowHandle(handle), handle);
        }

        private static Process GetProcessFromWindowHandle(IntPtr windowHandle)
        {
            NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

            if (processId == 0)
            {
                Injector.LogMessage($"Could not get process for window handle {windowHandle}");
                return null;
            }

            try
            {
                var process = Process.GetProcessById(processId);
                return process;
            }
            catch (Exception e)
            {
                Injector.LogMessage($"Could not get process for PID = {processId}.");
                Injector.LogMessage(e.ToString());
            }

            return null;
        }

        private static string GetTargetFramework(Process process)
        {
            var modules = NativeMethods.GetModules(process);

            var wpfgfx_cor3Found = false;
            FileVersionInfo hostFxrVersionInfo = null;

            foreach (var module in modules)
            {
                if (module.szModule.Equals("hostfxr.dll", StringComparison.OrdinalIgnoreCase))
                {
                    hostFxrVersionInfo = FileVersionInfo.GetVersionInfo(module.szExePath);
                }

                if (module.szModule.StartsWith("wpfgfx_cor3.dll", StringComparison.OrdinalIgnoreCase))
                {
                    wpfgfx_cor3Found = true;
                }

                if (wpfgfx_cor3Found
                    && !(hostFxrVersionInfo is null))
                {
                    break;
                }
            }

            if (wpfgfx_cor3Found)
            {
                if (hostFxrVersionInfo == null)
                {
                    return "netcoreapp3.0";
                }

                switch (hostFxrVersionInfo.FileMajorPart)
                {
                    case 3:
                        if (hostFxrVersionInfo.FileMinorPart >= 100)
                        {
                            return "netcoreapp3.1";
                        }
                        else
                        {
                            return "netcoreapp3.0";
                        }
                }
            }

            return "net40";
        }
    }
}