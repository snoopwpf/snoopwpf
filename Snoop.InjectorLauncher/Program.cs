// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.InjectorLauncher;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommandLine;
using Snoop.Data;
using Snoop.Infrastructure;

public static class Program
{
    public static int Main(string[] args)
    {
        var runResult = Parser.Default.ParseArguments<InjectorLauncherCommandLineOptions>(args)
            .MapResult(Run, _ => 1);

        return runResult;
    }

    private static int Run(InjectorLauncherCommandLineOptions commandLineOptions)
    {
        if (commandLineOptions.Debug)
        {
            Debugger.Launch();
        }

        if (commandLineOptions.AttachConsoleToParent)
        {
            ConsoleHelper.AttachConsoleToParentProcessOrAllocateNewOne();
        }

        Injector.LogMessage($"Starting the injection process for PID '{commandLineOptions.TargetPID}' and HWND '{commandLineOptions.TargetHwnd}'...", false);

        try
        {
            var processWrapper = ProcessWrapper.From(commandLineOptions.TargetPID, new IntPtr(commandLineOptions.TargetHwnd));

            if (processWrapper is null)
            {
                Injector.LogMessage($"Could not find process with ID \"{commandLineOptions.TargetPID}\" or something else went wrong while getting it's details.");
                return 1;
            }

            // Check for target process and our architecture.
            // If they don't match we redirect everything to the appropriate injector launcher.
            {
                using var currentProcess = Process.GetCurrentProcess();
                var currentProcessArchitecture = NativeMethods.GetArchitectureWithoutException(currentProcess);
                if (processWrapper.Architecture.Equals(currentProcessArchitecture, StringComparison.Ordinal) == false)
                {
                    Injector.LogMessage("Target process and injector process have different architectures, trying to redirect to secondary process...");

                    var originalProcessFileName = currentProcess.MainModule!.ModuleName!;
#pragma warning disable CA1307
                    var correctArchitectureFileName = originalProcessFileName.Replace(currentProcessArchitecture, processWrapper.Architecture);
                    var processStartInfo = new ProcessStartInfo(currentProcess.MainModule.FileName!.Replace(originalProcessFileName, correctArchitectureFileName), Parser.Default.FormatCommandLine(commandLineOptions))
                    {
                        CreateNoWindow = true,
                        WorkingDirectory = currentProcess.StartInfo.WorkingDirectory
                    };

                    using (var process = Process.Start(processStartInfo))
                    {
                        if (process is null)
                        {
                            Injector.LogMessage("Failed to start process for redirection.");
                            return 1;
                        }

                        process.WaitForExit();
                        return process.ExitCode;
                    }
                }
            }

            var settingsFile = string.IsNullOrEmpty(commandLineOptions.SettingsFile) == false
                ? commandLineOptions.SettingsFile
                : new TransientSettingsData
                {
                    StartTarget = SnoopStartTarget.SnoopUI,
                    TargetWindowHandle = processWrapper.WindowHandle.ToInt64()
                }.WriteToFile();

            var injectorData = new InjectorData
            {
                FullAssemblyPath = GetAssemblyPath(processWrapper, commandLineOptions.Assembly),
                ClassName = commandLineOptions.ClassName,
                MethodName = commandLineOptions.MethodName,
                SettingsFile = settingsFile
            };

            if (File.Exists(injectorData.FullAssemblyPath) == false)
            {
                Injector.LogMessage($"Could not find assembly \"{injectorData.FullAssemblyPath}\".");
                return 1;
            }

            try
            {
                Injector.InjectIntoProcess(processWrapper, injectorData);
            }
            catch (Exception exception)
            {
                Injector.LogMessage($"Failed to inject Snoop into process {processWrapper.Process.ProcessName} (PID = {processWrapper.Process.Id})");
                Injector.LogMessage(exception.ToString());
                return 1;
            }

            Injector.LogMessage($"Successfully injected Snoop into process {processWrapper.Process.ProcessName} (PID = {processWrapper.Process.Id})");

            return 0;
        }
        catch (Exception ex)
        {
            Injector.LogMessage(ex.ToString());
            return 1;
        }
    }

    private static string GetAssemblyPath(ProcessWrapper processWrapper, string assemblyNameOrFullPath)
    {
        if (File.Exists(assemblyNameOrFullPath)
            || Path.IsPathRooted(assemblyNameOrFullPath))
        {
            return assemblyNameOrFullPath;
        }

        var thisAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        return Path.Combine(thisAssemblyDirectory!, processWrapper.SupportedFrameworkName, $"{assemblyNameOrFullPath}.dll");
    }
}