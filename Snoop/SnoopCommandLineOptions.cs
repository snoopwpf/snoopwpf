namespace Snoop;

using CommandLine;

public interface IOptionsWithTarget
{
    [Option('t', "targetPID", Required = true, HelpText = "The target process id.")]
    int TargetPID { get; set; }

    [Option('h', "targetHwnd", HelpText = "The target window handle.")]
    int TargetHwnd { get; set; }
}

[Verb("inspect", HelpText = "Inspect the UI of target process.")]
public class InspectCommandLineOptions : BaseCommandLineOptions, IOptionsWithTarget
{
    public int TargetPID { get; set; }

    public int TargetHwnd { get; set; }
}

[Verb("magnify", HelpText = "Magnify the UI of target process.")]
public class MagnifyCommandLineOptions : BaseCommandLineOptions, IOptionsWithTarget
{
    public int TargetPID { get; set; }

    public int TargetHwnd { get; set; }
}

[Verb("run", HelpText = "Run snoop.")]
public class SnoopCommandLineOptions : BaseCommandLineOptions
{
}

public class BaseCommandLineOptions
{
    [Option('d', "debug")]
    public bool Debug { get; set; }

    [Option("showConsole", HelpText = "Shows the console even when the application is running with a GUI.")]
    public bool ShowConsole { get; set; }
}