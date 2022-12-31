namespace Snoop;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using CommandLine;
using Snoop.Core;
using Snoop.Infrastructure;

public static class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    public static bool IsConsoleApp { get; } = GetConsoleWindow() != IntPtr.Zero;

    public static bool Debug { get; set; }

    [STAThread]
    public static int Main(string[] args)
    {
        Environment.SetEnvironmentVariable(SettingsHelper.SNOOP_INSTALL_PATH_ENV_VAR, Path.GetDirectoryName(EnvironmentEx.CurrentProcessPath), EnvironmentVariableTarget.Process);

        var helpWriter = new StringWriter();

        var parser = new Parser(x => x.HelpWriter = helpWriter);

        return parser.ParseArguments<InspectCommandLineOptions, MagnifyCommandLineOptions, SnoopCommandLineOptions>(args)
            .MapResult(
                (InspectCommandLineOptions options) => Inspect(options),
                (MagnifyCommandLineOptions options) => Magnify(options),
                (SnoopCommandLineOptions options) => Run(options),
                errs => ErrorHandler(args, errs.ToList(), helpWriter));
    }

    private static int Inspect(InspectCommandLineOptions options)
    {
        var processInfo = new ProcessInfo(options.TargetPID);
        var result = processInfo.Snoop(new IntPtr(options.TargetHwnd));
        return result.Success
            ? 0
            : 1;
    }

    private static int Magnify(MagnifyCommandLineOptions options)
    {
        var processInfo = new ProcessInfo(options.TargetPID);
        var result = processInfo.Magnify(new IntPtr(options.TargetHwnd));
        return result.Success
            ? 0
            : 1;
    }

    private static int Run(SnoopCommandLineOptions options)
    {
        Debug = options.Debug;

        if (IsConsoleApp == false
            && options.ShowConsole)
        {
            NativeMethods.AllocConsole();
        }

        var app = new App();
        return app.Run();
    }

    private static int ErrorHandler(string[] args, IList<Error> errors, StringWriter helpWriter)
    {
        if (errors.Count == 1
            && errors.All(x => x is NoVerbSelectedError or BadVerbSelectedError))
        {
            var localHelpWriter = new StringWriter();
            var parser = new Parser(x => x.HelpWriter = localHelpWriter);

            return parser.ParseArguments<SnoopCommandLineOptions>(args).MapResult(
                Run,
                errs => ErrorHandler(args, errs.ToList(), localHelpWriter));
        }

        if (IsConsoleApp)
        {
            Console.Error.WriteLine(helpWriter.ToString());
        }
        else
        {
            ShowErrorsGui(helpWriter.ToString());
        }

        return 1;
    }

    private static void ShowErrorsGui(string helpText)
    {
        MessageBox.Show(helpText, "Error while parsing commandline", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}