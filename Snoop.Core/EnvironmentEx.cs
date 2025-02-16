namespace Snoop.Core;

using System;
using Snoop.Infrastructure;

#if NET
using System.IO;
#else
using System.Diagnostics;
#endif

public static class EnvironmentEx
{
    public static readonly string? CurrentProcessName;
    public static readonly string? CurrentProcessPath;

    static EnvironmentEx()
    {
#if NET
        CurrentProcessPath = Environment.ProcessPath;
        CurrentProcessName = OperatingSystem.IsWindows()
            ? Path.GetFileNameWithoutExtension(CurrentProcessPath) : Path.GetFileName(CurrentProcessPath);
#else
        using var currentProcess = Process.GetCurrentProcess();
        CurrentProcessName = currentProcess.ProcessName;
        CurrentProcessPath = GetProcessPath(currentProcess);
#endif
    }

#if !NET
    private static string? GetProcessPath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception e)
        {
            LogHelper.WriteError(e);
        }

        return string.Empty;
    }
#endif
}
