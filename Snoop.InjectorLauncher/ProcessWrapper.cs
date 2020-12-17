namespace Snoop.InjectorLauncher
{
    using System;
    using System.Diagnostics;
    using Snoop.Infrastructure;

    public class ProcessWrapper
    {
        public ProcessWrapper(Process process, IntPtr windowHandle)
        {
            this.Process = process ?? throw new ArgumentNullException(nameof(process));
            this.Id = process.Id;
            this.Handle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.All, false, process.Id);
            this.WindowHandle = windowHandle;

            this.Bitness = GetBitnessAsString(this.Process);

            this.SupportedFrameworkName = GetSupportedTargetFramework(process);
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

        public static ProcessWrapper? From(int processId, IntPtr windowHandle)
        {
            var processFromId = Process.GetProcessById(processId);

            if (processFromId is null)
            {
                return null;
            }

            return new ProcessWrapper(processFromId, windowHandle);
        }

        public static ProcessWrapper? FromWindowHandle(IntPtr handle)
        {
            var processFromWindowHandle = GetProcessFromWindowHandle(handle);

            if (processFromWindowHandle is null)
            {
                return null;
            }

            return new ProcessWrapper(processFromWindowHandle, handle);
        }

        private static Process? GetProcessFromWindowHandle(IntPtr windowHandle)
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

        private static string GetSupportedTargetFramework(Process process)
        {
            var modules = NativeMethods.GetModules(process);

            var wpfgfx_cor3Found = false;
            FileVersionInfo? hostPolicyVersionInfo = null;

            foreach (var module in modules)
            {
#if DEBUG
                Trace.WriteLine(module.szExePath);
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(module.szExePath);
                Trace.WriteLine($"File: {fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}");
                Trace.WriteLine($"Prod: {fileVersionInfo.ProductMajorPart}.{fileVersionInfo.ProductMinorPart}");
#endif

                if (module.szModule.StartsWith("hostpolicy.dll", StringComparison.OrdinalIgnoreCase))
                {
                    hostPolicyVersionInfo = FileVersionInfo.GetVersionInfo(module.szExePath);
                }

                if (module.szModule.StartsWith("wpfgfx_cor3.dll", StringComparison.OrdinalIgnoreCase))
                {
                    wpfgfx_cor3Found = true;
                }

                if (wpfgfx_cor3Found
                    && hostPolicyVersionInfo != null)
                {
                    break;
                }
            }

            if (wpfgfx_cor3Found)
            {
                switch (hostPolicyVersionInfo?.ProductMajorPart)
                {
                    case 5:
                        // we currently map from net 5 to netcoreapp 3.1
                        return "netcoreapp3.1"; //return "net5.0";

                    case 3 when hostPolicyVersionInfo.ProductMinorPart >= 1:
                        return "netcoreapp3.1";

                    case 3:
                        return "netcoreapp3.0";

                    default:
                        return "netcoreapp3.0";
                }
            }

            return "net40";
        }
    }
}