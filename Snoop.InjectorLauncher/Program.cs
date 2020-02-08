// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.InjectorLauncher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using CommandLine;
    using Snoop.Data;

    public static class Program
    {
        public static int Main(string[] args)
        {
            Injector.LogMessage("Starting the injection process...", false);

            InjectorLauncherCommandLineOptions commandLineOptions = null;
            Parser.Default.ParseArguments<InjectorLauncherCommandLineOptions>(args)
                .WithParsed(options => commandLineOptions = options);

            return Run(commandLineOptions);
        }

        private static int Run(InjectorLauncherCommandLineOptions commandLineOptions)
        {
            try
            {
                if (commandLineOptions.Debug)
                {
                    Debugger.Launch();
                }

                var processWrapper = ProcessWrapper.From(commandLineOptions.Target);

                // Check for target process and our bitness.
                // If they don't match we redirect everything to the appropriate injector launcher.
                {
                    var currentProcess = Process.GetCurrentProcess();
                    var currentProcessBitness = ProcessWrapper.GetBitnessAsString(currentProcess);
                    if (processWrapper.Bitness.Equals(currentProcessBitness) == false)
                    {
                        Injector.LogMessage("Target process and injector process have different bitness, trying to redirect to secondary process...");

                        Debugger.Launch();

                        var originalProcessFileName = currentProcess.MainModule.ModuleName;
                        var correctBitnessFileName = originalProcessFileName.Replace(currentProcessBitness, processWrapper.Bitness);
                        var processStartInfo = new ProcessStartInfo(currentProcess.MainModule.FileName.Replace(originalProcessFileName, correctBitnessFileName), Parser.Default.FormatCommandLine(commandLineOptions))
                        {
                            CreateNoWindow = true,
                            WorkingDirectory = currentProcess.StartInfo.WorkingDirectory
                        };

                        using (var process = Process.Start(processStartInfo))
                        {
                            if (process == null)
                            {
                                Injector.LogMessage("Failed to start process for redirection.");
                                return 1;
                            }

                            process.WaitForExit();
                            return process.ExitCode;
                        }
                    }
                }

                var assemblyNameOrFullPath = commandLineOptions.Assembly;
                var className = commandLineOptions.ClassName;
                var methodName = commandLineOptions.MethodName;
                var settingsFile = string.IsNullOrEmpty(commandLineOptions.SettingsFile) == false
                    ? commandLineOptions.SettingsFile
                    : new TransientSettingsData
                    {
                        StartTarget = SnoopStartTarget.SnoopUI,
                        TargetWindowHandle = processWrapper.WindowHandle.ToInt64()
                    }.WriteToFile();

                var injectorData = new InjectorData
                {
                    FullAssemblyPath = GetAssemblyPath(processWrapper, assemblyNameOrFullPath),
                    ClassName = className,
                    MethodName = methodName,
                    SettingsFile = settingsFile
                };

                if (File.Exists(injectorData.FullAssemblyPath) == false)
                {
                    Injector.LogMessage($"Could not find \"{injectorData.FullAssemblyPath}\".");
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
            if (File.Exists(assemblyNameOrFullPath))
            {
                return assemblyNameOrFullPath;
            }

            var thisAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(thisAssemblyDirectory, processWrapper.SupportedFrameworkName, $"{assemblyNameOrFullPath}.dll");
        }
    }
}