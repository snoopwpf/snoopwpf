// ReSharper disable once CheckNamespace
namespace Snoop.Core;

using System;
using System.Diagnostics;
using System.IO;

public static class SettingsHelper
{
    private static readonly string currentProcessPath;
    private static readonly string currentProcessName;

    static SettingsHelper()
    {
        using var currentProcess = Process.GetCurrentProcess();
        currentProcessName = currentProcess.ProcessName;
        currentProcessPath = currentProcess.MainModule!.FileName!;
    }

    public static string GetSnoopAppDataPath()
    {
        var snoopAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Snoop");

        if (Directory.Exists(snoopAppDataPath) == false)
        {
            Directory.CreateDirectory(snoopAppDataPath);
        }

        return snoopAppDataPath;
    }

    public static string GetSettingsFileForSnoop()
    {
        var snoopAppDataPath = GetSnoopAppDataPath();

        var settingsFile = Path.Combine(snoopAppDataPath, "SnoopSettings.xml");

        return settingsFile;
    }

    public static string GetSettingsFileForCurrentProcess()
    {
        var snoopAppDataPath = GetSnoopAppDataPath();

        // Try to find application specific settings
        {
            var settingsPath = Path.Combine(snoopAppDataPath, currentProcessName);
            var settingsFile = Path.Combine(settingsPath, "Settings.xml");

            if (Directory.Exists(settingsPath)
                && File.Exists(settingsFile))
            {
                return settingsFile;
            }
        }

        // Try to to find mapped application settings
        {
            var settingsFileFromMap = SettingsFileMap.Default.GetSettingsFile(currentProcessName, currentProcessPath);

            if (string.IsNullOrEmpty(settingsFileFromMap) == false
                && File.Exists(settingsFileFromMap))
            {
                return settingsFileFromMap;
            }
        }

        // Use default settings
        return Path.Combine(snoopAppDataPath, "DefaultSettings.xml");
    }
}