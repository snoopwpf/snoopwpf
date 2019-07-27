using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

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

        if (IsLocalBuild == false) {        
            GitVersionTasks.GitVersion(s => s.SetOutput(GitVersionOutput.buildserver));
        }
    }

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath CurrentBuildOutputDirectory => BuildBinDirectory / Configuration;

    AbsolutePath IntermediateOutputDirectory => RootDirectory / "";

    AbsolutePath OutputDirectory => RootDirectory / "output";

    AbsolutePath ChocolateyDirectory => RootDirectory / "chocolatey";

    AbsolutePath PaketDirectory => RootDirectory / ".paket";

    AbsolutePath PackagesDirectory => RootDirectory / "packages";

    AbsolutePath WixDirectory => PackagesDirectory / "wix/tools";

    Target Clean => _ => _
        .DependsOn(CleanOutput)
        .Executes(() =>
        {
            // EnsureCleanDirectory(OutputDirectory);
        });

    Target CleanOutput => _ => _
        .Executes(() =>
                  {
                      EnsureCleanDirectory(OutputDirectory);
                  });

    Target Restore => _ => _
        .Executes(() =>
        {
            ProcessTasks.StartProcess(PaketDirectory / "paket.exe", "restore");

            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.GetNormalizedAssemblyVersion())
                .SetFileVersion(GitVersion.GetNormalizedFileVersion())
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .Executes(() =>
        {
            //   & $nuget pack "$(Join-Path $PSScriptRoot 'chocolatey\snoop.nuspec')" -Version $version -Properties Configuration=$Configuration -OutputDirectory "$outputDirectory" -NoPackageAnalysis
            NuGetTasks.NuGetPack(s => s
                .SetTargetPath(ChocolateyDirectory / "snoop.nuspec")
                .SetVersion(GitVersion.NuGetVersion)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(OutputDirectory)
                .SetNoPackageAnalysis(true));

            var tempDirectory = TemporaryDirectory / "XX";

            var nupkgs = OutputDirectory.GlobFiles("*.nupkg");

            CompressionTasks.UncompressZip(nupkgs.First(), tempDirectory);

            CompressionTasks.Compress(tempDirectory / "tools",  OutputDirectory / $"snoop.{GitVersion.NuGetVersion}.zip", info => info.Name.Contains("chocolatey") == false && info.Name != "VERIFICATION.txt");
        });

    Target Setup => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .Executes(() =>
                  {
                      ProcessTasks.StartProcess(WixDirectory / "candle.exe", 
                                                $"snoop.wxs -ext WixUIExtension -o \"{OutputDirectory / "Snoop.wixobj"}\" -dProductVersion=\"{GitVersion.MajorMinorPatch}\" -nologo")
                                  .WaitForExit();

                      ProcessTasks.StartProcess(WixDirectory / "light.exe", 
                                                $"-out \"{OutputDirectory / $"Snoop.{GitVersion.NuGetVersion}.msi"}\" -b \"{CurrentBuildOutputDirectory}\" \"{OutputDirectory / "Snoop.wixobj"}\" -ext WixUIExtension -dProductVersion=\"{GitVersion.MajorMinorPatch}\" -pdbout \"{OutputDirectory / "Snoop.wixpdb"}\" -nologo -sice:ICE61")
                                  .WaitForExit();
                  });

    Target CI => _ => _
        .DependsOn(Compile, Pack, Setup);
}
