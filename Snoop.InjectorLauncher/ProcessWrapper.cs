namespace Snoop.InjectorLauncher;

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
        catch (Exception exception)
        {
            Injector.LogMessage(exception.ToString());
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

        FileVersionInfo? systemRuntimeVersion = null;
        FileVersionInfo? wpfGFXVersion = null;

        foreach (var module in modules)
        {
#if DEBUG
            Injector.LogMessage(module.szExePath);
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(module.szExePath);
            Injector.LogMessage($"File: {fileVersionInfo.FileVersion}");
            Injector.LogMessage($"Prod: {fileVersionInfo.ProductVersion}");
#endif

            if (module.szModule.StartsWith("wpfgfx_", StringComparison.OrdinalIgnoreCase))
            {
                wpfGFXVersion = FileVersionInfo.GetVersionInfo(module.szExePath);
            }
            else if (module.szModule.StartsWith("System.Runtime.dll", StringComparison.OrdinalIgnoreCase))
            {
                systemRuntimeVersion = FileVersionInfo.GetVersionInfo(module.szExePath);
            }
        }

        var relevantVersionInfo = systemRuntimeVersion
            ?? wpfGFXVersion;

        if (relevantVersionInfo is null)
        {
            return "net462";
        }

        var productVersion = TryParseVersion(relevantVersionInfo.ProductVersion ?? string.Empty);
        return productVersion.Major switch
        {
            >= 6 => "net6.0-windows",
            4 => "net462",
            _ => throw new NotSupportedException($".NET version {relevantVersionInfo.ProductVersion} is not supported.")
        };
    }

    private static Version TryParseVersion(string version)
    {
        var versionToParse = version;

        var previewVersionMarkerIndex = versionToParse.IndexOfAny(new[] { '-', '+' });

        if (previewVersionMarkerIndex > -1)
        {
            versionToParse = version.Substring(0, previewVersionMarkerIndex);
        }

        if (Version.TryParse(versionToParse, out var parsedVersion))
        {
            return parsedVersion;
        }

        return new Version();
    }
}