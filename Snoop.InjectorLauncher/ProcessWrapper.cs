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

            this.Architecture = NativeMethods.GetArchitectureWithoutException(this.Process);

            this.SupportedFrameworkName = GetSupportedTargetFramework(process);
        }

        public Process Process { get; }

        public int Id { get; }

        public NativeMethods.ProcessHandle Handle { get; }

        public IntPtr WindowHandle { get; }

        public string Architecture { get; }

        public string SupportedFrameworkName { get; }

        public static ProcessWrapper? From(int processId, IntPtr windowHandle)
        {
            try
            {
                var processFromId = Process.GetProcessById(processId);

                return new ProcessWrapper(processFromId, windowHandle);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return null;
            }
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
            _ = NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);

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

            // ReSharper disable once IdentifierTypo
            // ReSharper disable once InconsistentNaming
            var wpfGfxForCoreFrameworkFound = false;
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

                if (module.szModule.StartsWith("wpfgfx_cor3.dll", StringComparison.OrdinalIgnoreCase)
                    || module.szModule.StartsWith("wpfgfx_net6.dll", StringComparison.OrdinalIgnoreCase))
                {
                    wpfGfxForCoreFrameworkFound = true;
                }

                if (wpfGfxForCoreFrameworkFound
                    && hostPolicyVersionInfo is not null)
                {
                    break;
                }
            }

            if (wpfGfxForCoreFrameworkFound)
            {
                switch (hostPolicyVersionInfo?.ProductMajorPart)
                {
                    case 6:
                        return "net6.0-windows";

                    case 5:
                        return "net5.0-windows";

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