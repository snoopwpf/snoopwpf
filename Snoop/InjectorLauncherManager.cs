// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using Snoop.Data;
using Snoop.Infrastructure;
using Snoop.InjectorLauncher;

/// <summary>
/// Class responsible for launching a new injector process.
/// </summary>
public static class InjectorLauncherManager
{
    public static void Launch(ProcessInfo processInfo, IntPtr targetHwnd, MethodInfo methodInfo, TransientSettingsData transientSettingsData)
    {
        Launch(processInfo, targetHwnd, methodInfo.DeclaringType!.Assembly.GetName().Name, methodInfo.DeclaringType.FullName!, methodInfo.Name, transientSettingsData.WriteToFile());
    }

    public static void Launch(ProcessInfo processInfo, IntPtr targetHwnd, string assembly, string className, string methodName, string transientSettingsFile)
    {
        if (File.Exists(transientSettingsFile) == false)
        {
            throw new FileNotFoundException("The generated temporary settings file could not be found.", transientSettingsFile);
        }

        try
        {
            var location = Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(location) ?? string.Empty;
            // If we get the architecture wrong here the InjectorLauncher will fix this by starting a secondary instance.
            var architecture = NativeMethods.GetArchitectureWithoutException(processInfo.Process);
            var injectorLauncherExe = Path.Combine(directory, $"Snoop.InjectorLauncher.{architecture}.exe");

            if (File.Exists(injectorLauncherExe) is false)
            {
                var message = @$"Could not find the injector launcher ""{injectorLauncherExe}"".
Snoop requires this component, which is part of the Snoop project, to do it's job.
- If you compiled snoop yourself, you should compile all required components.
- If you downloaded snoop you should not omit any files contained in the archive you downloaded and make sure that no anti virus deleted the file.";
                throw new FileNotFoundException(message, injectorLauncherExe);
            }

            var injectorLauncherCommandLineOptions = new InjectorLauncherCommandLineOptions
            {
                TargetPID = processInfo.Process.Id,
                TargetHwnd = targetHwnd.ToInt32(),
                Assembly = assembly,
                ClassName = className,
                MethodName = methodName,
                SettingsFile = transientSettingsFile,
                Debug = Program.Debug,
                AttachConsoleToParent = true
            };

            var commandLine = Parser.Default.FormatCommandLine(injectorLauncherCommandLineOptions);
            var processStartInfo = new ProcessStartInfo(injectorLauncherExe, commandLine)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = Program.Debug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                Verb = processInfo.IsProcessElevated
                    ? "runas"
                    : null,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            LogHelper.WriteLine($"Launching injector \"{processStartInfo.FileName}\".");
            LogHelper.WriteLine($"Arguments: {commandLine}.");

            using var process = new Process();
            process.StartInfo = processStartInfo;

            // Subscribe to output streams
            process.OutputDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    LogHelper.WriteLine($"[Injector Output] {args.Data}");
                }
            };

            process.ErrorDataReceived += (_, args) =>
            {
                if (args.Data is not null)
                {
                    LogHelper.WriteLine($"[Injector Error] {args.Data}");
                }
            };

            if (process.Start())
            {
                // Begin reading output/error streams asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                LogHelper.WriteLine($"Injector returned exit code: {process.ExitCode}.");
            }
            else
            {
                LogHelper.WriteLine("Injector could not be started.");
            }
        }
        finally
        {
            File.Delete(transientSettingsFile);
        }
    }
}