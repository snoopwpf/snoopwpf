namespace Snoop.Core;

using System;
using System.Diagnostics;
using Snoop.Infrastructure;

public static class EnvironmentEx
{
    public static readonly string CurrentProcessName;
    public static readonly string? CurrentProcessPath;

    static EnvironmentEx()
    {
        using var currentProcess = Process.GetCurrentProcess();
        CurrentProcessName = currentProcess.ProcessName;
        CurrentProcessPath = GetProcessPath(currentProcess);
    }

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
}