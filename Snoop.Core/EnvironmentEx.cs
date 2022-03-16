namespace Snoop.Core;

using System.Diagnostics;

public static class EnvironmentEx
{
    public static readonly string CurrentProcessPath;
    public static readonly string CurrentProcessName;

    static EnvironmentEx()
    {
        using var currentProcess = Process.GetCurrentProcess();
        CurrentProcessName = currentProcess.ProcessName;
        CurrentProcessPath = currentProcess.MainModule!.FileName!;
    }
}