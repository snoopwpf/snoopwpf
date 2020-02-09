namespace Snoop.InjectorLauncher
{
    using CommandLine;

    public class InjectorLauncherCommandLineOptions
    {
        [Option('t', "targetPID", Required = true, HelpText = "The target process id.")]
        public int TargetPID { get; set; }

        [Option('h', "targetHwnd", HelpText = "The target window handle.")]
        public int TargetHwnd { get; set; }

        [Option('a', "assembly", Required = true)]
        public string Assembly { get; set; }

        [Option('c', "className", Required = true)]
        public string ClassName { get; set; }

        [Option('m', "methodName", Required = true)]
        public string MethodName { get; set; }

        [Option('s', "settingsFile")]
        public string SettingsFile { get; set; }

        [Option('v', "verbose", HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('d', "debug")]
        public bool Debug { get; set; }
    }
}