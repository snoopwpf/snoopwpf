// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using CommandLine;
    using Snoop.Data;
    using Snoop.InjectorLauncher;

    /// <summary>
    /// Class responsible for launching a new injector process.
    /// </summary>
    public static class InjectorLauncherManager
    {
        private static string GetSuffix(WindowInfo windowInfo)
        {
            var bitness = windowInfo.IsOwningProcess64Bit
                ? "x64"
                : "x86";

            return bitness;
        }

        public static void Launch(WindowInfo windowInfo, MethodInfo methodInfo, TransientSettingsData transientSettingsData)
        {
            Launch(windowInfo, methodInfo.DeclaringType.Assembly.GetName().Name, methodInfo.DeclaringType.FullName, methodInfo.Name, transientSettingsData.WriteToFile());
        }

        public static void Launch(WindowInfo windowInfo, string assembly, string className, string methodName, string transientSettingsFile)
        {
            if (File.Exists(transientSettingsFile) == false)
            {
                throw new FileNotFoundException("The generated temporary settings file could not be found.", transientSettingsFile);
            }

            try
            {
                var location = Assembly.GetExecutingAssembly().Location;
                var directory = Path.GetDirectoryName(location) ?? string.Empty;
                var injectorLauncherExe = Path.Combine(directory, $"Snoop.InjectorLauncher.{GetSuffix(windowInfo)}.exe");

                if (File.Exists(injectorLauncherExe) == false)
                {
                    var message = @$"Could not find the injector launcher ""{injectorLauncherExe}"".
Snoop requires this component, which is part of the Snoop project, to do it's job.
- If you compiled snoop yourself, you should compile all required components.
- If you downloaded snoop you should not omit any files contained in the archive you downloaded and make sure that no anti virus deleted the file.";
                    throw new FileNotFoundException(message, injectorLauncherExe);
                }

                var injectorLauncherCommandLineOptions = new InjectorLauncherCommandLineOptions
                {
                    Target = $"{windowInfo.OwningProcess.Id}:{windowInfo.HWnd}",
                    Assembly = assembly,
                    ClassName = className,
                    MethodName = methodName,
                    SettingsFile = transientSettingsFile
                };

                var commandLine = CommandLine.Parser.Default.FormatCommandLine(injectorLauncherCommandLineOptions);
                var startInfo = new ProcessStartInfo(injectorLauncherExe, commandLine)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = windowInfo.IsOwningProcessElevated
                                               ? "runas"
                                               : null
                };

                using var process = Process.Start(startInfo);
                process?.WaitForExit();
            }
            finally
            {
                File.Delete(transientSettingsFile);
            }
        }
    }
}