using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    static Build()
    {
        // generated with http://patorjk.com/software/taag/#p=display&f=ANSI%20Shadow&t=SnoopWPF
        Logger.Normal("███████╗███╗   ██╗ ██████╗  ██████╗ ██████╗ ██╗    ██╗██████╗ ███████╗");
        Logger.Normal("██╔════╝████╗  ██║██╔═══██╗██╔═══██╗██╔══██╗██║    ██║██╔══██╗██╔════╝");
        Logger.Normal("███████╗██╔██╗ ██║██║   ██║██║   ██║██████╔╝██║ █╗ ██║██████╔╝█████╗  ");
        Logger.Normal("╚════██║██║╚██╗██║██║   ██║██║   ██║██╔═══╝ ██║███╗██║██╔═══╝ ██╔══╝  ");
        Logger.Normal("███████║██║ ╚████║╚██████╔╝╚██████╔╝██║     ╚███╔███╔╝██║     ██║     ");
        Logger.Normal("╚══════╝╚═╝  ╚═══╝ ╚═════╝  ╚═════╝ ╚═╝      ╚══╝╚══╝ ╚═╝     ╚═╝     ");
    }

    public static int Main() => Execute<Build>(x => x.Compile);

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();

        ProcessTasks.DefaultLogInvocation = true;
        ProcessTasks.DefaultLogOutput = true;

        Console.WriteLine("IsLocalBuild           : {0}", IsLocalBuild);
        Console.WriteLine("Informational   Version: {0}", GitVersion.InformationalVersion);
        Console.WriteLine("SemVer          Version: {0}", GitVersion.SemVer);
        Console.WriteLine("AssemblySemVer  Version: {0}", GitVersion.AssemblySemVer);
        Console.WriteLine("MajorMinorPatch Version: {0}", GitVersion.MajorMinorPatch);
        Console.WriteLine("NuGet           Version: {0}", GitVersion.NuGetVersion);
    }

    string ProjectName = "Snoop";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;

    [GitRepository] readonly GitRepository GitRepository;

    [GitVersion(Framework = "netcoreapp3.1")] readonly GitVersion GitVersion;
    
    readonly List<string> CheckSumFiles = new List<string>();

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath CurrentBuildOutputDirectory => BuildBinDirectory / Configuration;

    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    AbsolutePath ChocolateyDirectory => RootDirectory / "chocolatey";

    string CandleExecutable => ToolPathResolver.GetPackageExecutable("wix", "candle.exe");

    string LightExecutable => ToolPathResolver.GetPackageExecutable("wix", "light.exe");

    readonly string FenceOutput = "".PadLeft(30, '#');

    Target CleanOutput => _ => _
        .Executes(() => {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() => {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => {
            MSBuild(s => s
                .SetProjectFile(RootDirectory / "Snoop.GenericInjector/Snoop.GenericInjector.vcxproj")
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.Win32)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .DisableRestore()
                .SetVerbosity(MSBuildVerbosity.Minimal));

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration("CI_" + Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                
                .SetVerbosity(DotNetVerbosity.Minimal));
        });

    Target CompileTestHarnesses => _ => _
        .Executes(() =>
        {
            MSBuild(s => s
                .SetProjectFile(RootDirectory / "TestHarnesses/Win32ToWPFInterop/Win32Clock/win32clock.vcxproj")
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.Win32)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .DisableRestore()
                .SetVerbosity(MSBuildVerbosity.Minimal));

            DotNetBuild(s => s
                .SetProjectFile(RootDirectory / "TestHarnesses/TestHarnesses.sln")
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetVerbosity(DotNetVerbosity.Minimal));
        });

    Target Pack => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .Executes(() => {
            // Generate ingore files to prevent chocolatey from generating shims for them
            foreach (var launcher in CurrentBuildOutputDirectory.GlobFiles($"{ProjectName}.InjectorLauncher.*.exe"))
            {
                using (File.Create(launcher + ".ignore")) { };
            }

            NuGetTasks.NuGetPack(s => s
                .SetTargetPath(ChocolateyDirectory / $"{ProjectName}.nuspec")
                .SetVersion(GitVersion.NuGetVersion)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetNoPackageAnalysis(true));
            
            var tempDirectory = TemporaryDirectory / $"{ProjectName}{nameof(Pack)}";

            EnsureCleanDirectory(tempDirectory);

            var nupkg = ArtifactsDirectory / $"{ProjectName}.{GitVersion.NuGetVersion}.nupkg";

            CheckSumFiles.Add(nupkg);

            {
                CompressionTasks.UncompressZip(nupkg, tempDirectory);

                var outputFile = ArtifactsDirectory / $"{ProjectName}.{GitVersion.NuGetVersion}.zip";
                CompressionTasks.Compress(tempDirectory / "tools",  outputFile, info => info.Name.Contains("chocolatey") == false && info.Name != "VERIFICATION.txt");
                CheckSumFiles.Add(outputFile);
            }
        });

    Target Setup => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .After(Pack)
        .Executes(() => {
            var tempDirectory = TemporaryDirectory / $"{ProjectName}{nameof(Setup)}";

            EnsureCleanDirectory(tempDirectory);

            var candleProcess = ProcessTasks.StartProcess(CandleExecutable, 
                $"{ProjectName}.wxs -ext WixUIExtension -o \"{tempDirectory / $"{ProjectName}.wixobj"}\" -dProductVersion=\"{GitVersion.MajorMinorPatch}\" -nologo");
            candleProcess.AssertZeroExitCode();

            var outputFile = $"{ArtifactsDirectory / $"{ProjectName}.{GitVersion.NuGetVersion}.msi"}";
            var lightProcess = ProcessTasks.StartProcess(LightExecutable, 
                $"-out \"{outputFile}\" -b \"{CurrentBuildOutputDirectory}\" \"{tempDirectory / $"{ProjectName}.wixobj"}\" -ext WixUIExtension -dProductVersion=\"{GitVersion.MajorMinorPatch}\" -pdbout \"{tempDirectory / $"{ProjectName}.wixpdb"}\" -nologo -sice:ICE61");
            lightProcess.AssertZeroExitCode();

            CheckSumFiles.Add(outputFile);
        });

    Target CheckSums => _ => _
        .TriggeredBy(Pack, Setup)
        .Executes(() => {
            foreach (var item in CheckSumFiles)
            {
                var checkSum = FileHelper.SHA256CheckSum(item);
                Logger.Info(FenceOutput);
                Logger.Info($"CheckSum for \"{item}\".");
                Logger.Info($"SHA256 \"{checkSum}\".");
                File.WriteAllText(item + ".sha256", checkSum);
                Logger.Info(FenceOutput);
            }
        });

    Target CI => _ => _
        .DependsOn(Compile, Pack, Setup);
}