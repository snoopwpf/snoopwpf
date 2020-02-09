namespace Snoop.InjectorLauncher
{
    using CommandLine;

    public class InjectorLauncherCommandLineOptions
    {
        [Option('t', "TargetPID", Required = true, HelpText = "The target process id.")]
        public int TargetPID { get; set; }

        [Option('h', "TargetHwnd", HelpText = "The target window handle.")]
        public int TargetHwnd { get; set; }

        [Option('a', "Assembly", Required = true)]
        public string Assembly { get; set; }

        [Option('c', "ClassName", Required = true)]
        public string ClassName { get; set; }

        [Option('m', "MethodName", Required = true)]
        public string MethodName { get; set; }

        [Option('s', "SettingsFile")]
        public string SettingsFile { get; set; }

        [Option('v', "verbose", HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option('d', "debug")]
        public bool Debug { get; set; }
    }
}