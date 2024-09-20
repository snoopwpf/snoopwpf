// ReSharper disable once CheckNamespace
namespace Snoop.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class SettingsHelper
{
#pragma warning disable SA1310
    public const string SNOOP_INSTALL_PATH_ENV_VAR = "SNOOP_INSTALL_PATH";
    public const string SNOOP_SETTINGS_PATH_ENV_VAR = "SNOOP_SETTINGS_PATH";
    public const string SNOOP_SETTINGS_DIRECTORY_NAME = ".Snoop";
    public const string DEFAULT_APPLICATION_SETTINGS_FILENAME = "DefaultSettings.xml";
#pragma warning restore SA1310

    public static string GetSettingsRootPath()
    {
        return GetPotentialSettingsPaths().First();
    }

    public static string GetSettingsFileForSnoop()
    {
        var file = "SnoopSettings.xml";
        return FindExistingSettingsFile(file)
               ?? Path.Combine(GetPotentialSettingsPaths().First(), file);
    }

    public static string GetDefaultApplicationSettingsFile()
    {
        var file = DEFAULT_APPLICATION_SETTINGS_FILENAME;
        return FindExistingSettingsFile(file)
               ?? Path.Combine(GetPotentialSettingsPaths().First(), file);
    }

    public static string GetApplicationSpecificSettingsFile()
    {
        var file = $"AppSettings\\{EnvironmentEx.CurrentProcessName}.xml";
        return FindExistingSettingsFile(file)
            ?? Path.Combine(GetPotentialSettingsPaths().First(), file);
    }

    public static string GetSettingsFileForCurrentApplication()
    {
        // Try to find application specific settings
        {
            var settingsFile = GetApplicationSpecificSettingsFile();

            if (string.IsNullOrEmpty(settingsFile) == false
                && File.Exists(settingsFile))
            {
                return settingsFile;
            }
        }

        // Use default settings
        return GetDefaultApplicationSettingsFile();
    }

    public static string? FindExistingSettingsFile(string file)
    {
        foreach (var path in GetPotentialSettingsPaths())
        {
            var filePath = Path.Combine(path, file);

            if (File.Exists(filePath))
            {
                return filePath;
            }
        }

        return null;
    }

    public static IEnumerable<string> GetPotentialSettingsPaths()
    {
        // 1. from environment variable
        {
            var path = Environment.GetEnvironmentVariable(SNOOP_SETTINGS_PATH_ENV_VAR);

            if (string.IsNullOrEmpty(path) == false
                && Directory.Exists(path))
            {
                yield return path;
            }
        }

        // 2. from ".Snoop" folders
        {
            var startPath = EnvironmentEx.CurrentProcessPath;
            var currentPath = startPath;

            if (string.IsNullOrEmpty(currentPath) is false)
            {
                while (true)
                {
                    var path = Path.Combine(currentPath, SNOOP_SETTINGS_DIRECTORY_NAME);

                    if (Directory.Exists(path))
                    {
                        yield return path;
                    }

                    currentPath = Path.GetDirectoryName(currentPath);

                    if (string.IsNullOrEmpty(currentPath))
                    {
                        break;
                    }
                }
            }
        }

        // 3. Snoop install directory
        {
            var snoopInstallPath = Environment.GetEnvironmentVariable(SNOOP_INSTALL_PATH_ENV_VAR);

            if (string.IsNullOrEmpty(snoopInstallPath) == false)
            {
                var path = Path.Combine(snoopInstallPath, SNOOP_SETTINGS_DIRECTORY_NAME);

                if (Directory.Exists(path))
                {
                    yield return path;
                }
            }
        }

        // 4. AppData
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Snoop");

            // Don't check for existence here
            yield return path;
        }
    }
}