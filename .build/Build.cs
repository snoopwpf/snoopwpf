#pragma warning disable IDE0051 // Remove unused private members

using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.SignPath;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[GitHubActions(
    "deployment",
    GitHubActionsImage.WindowsLatest,
    OnPushBranches = new[] { MasterBranch, ReleaseBranchPrefix + "/*" },
    InvokedTargets = new[] { nameof(CI) },
    AutoGenerate = false
)]
[GitHubActions(
    "continuous",
    GitHubActionsImage.WindowsLatest,
    OnPushBranchesIgnore = new[] { MasterBranch, ReleaseBranchPrefix + "/*" },
    OnPullRequestBranches = new[] { DevelopBranch },
    InvokedTargets = new[] { nameof(CI) },
    AutoGenerate = false
)]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    const string MasterBranch = "master";
    const string DevelopBranch = "develop";
    const string ReleaseBranchPrefix = "tags";

    public static int Main() => Execute<Build>(x => x.Compile);

    protected override void OnBuildInitialized()
    {
        base.OnBuildInitialized();

        ProcessTasks.DefaultLogInvocation = true;
        ProcessTasks.DefaultLogOutput = true;

        if (GitVersion is null
            && IsLocalBuild == false)
        {
            throw new Exception("Could not initialize GitVersion.");
        }

        Console.WriteLine("IsLocalBuild           : {0}", IsLocalBuild.ToString());

        Console.WriteLine("Informational   Version: {0}", InformationalVersion);
        Console.WriteLine("SemVer          Version: {0}", SemVer);
        Console.WriteLine("AssemblySemVer  Version: {0}", AssemblySemVer);
        Console.WriteLine("MajorMinorPatch Version: {0}", MajorMinorPatch);
        Console.WriteLine("NuGet           Version: {0}", NuGetVersion);
    }

    string ProjectName = "Snoop";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution = null!;
    [Solution(GenerateProjects = true)] readonly TestHarnessSolution TestHarnessSolution = null!;

    [GitVersion(Framework = "netcoreapp3.1", NoFetch = true, NoCache = true)] readonly GitVersion? GitVersion;

    string AssemblySemVer => GitVersion?.AssemblySemVer ?? "1.0.0";
    string SemVer => GitVersion?.SemVer ?? "1.0.0";
    string InformationalVersion => GitVersion?.InformationalVersion ?? "1.0.0";
    string NuGetVersion => GitVersion?.NuGetVersion ?? "1.0.0";
    string MajorMinorPatch => GitVersion?.MajorMinorPatch ?? "1.0.0";

    [CI] readonly GitHubActions? GitHubActions;

    readonly List<string> CheckSumFiles = new();

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath CurrentBuildOutputDirectory => BuildBinDirectory / Configuration;

    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    AbsolutePath OutputDirectory => RootDirectory / "output";

    AbsolutePath ChocolateyDirectory => RootDirectory / "chocolatey";

    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";

    string CandleExecutable => ToolPathResolver.GetPackageExecutable("wix", "candle.exe");

    string LightExecutable => ToolPathResolver.GetPackageExecutable("wix", "light.exe");

    readonly string FenceOutput = "".PadLeft(30, '#');

    Target CleanOutput => _ => _
        .Executes(() =>
        {
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));

            DotNetRestore(s => s
                .SetProjectFile(TestHarnessSolution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetProjectFile(Solution.Snoop_GenericInjector)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.Win32)
                .SetAssemblyVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)
                .DisableRestore()
                .SetVerbosity(MSBuildVerbosity.Minimal));

            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration("CI_" + Configuration)
                .SetAssemblyVersion(AssemblySemVer)
                .SetFileVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)
                .SetVerbosity(DotNetVerbosity.Minimal));
        });

    [PublicAPI]
    Target CompileTestHarnesses => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(TestHarnessSolution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)
                .SetNoRestore(true)
                .SetVerbosity(DotNetVerbosity.Minimal));

            MSBuild(s => s
                .SetProjectFile(TestHarnessSolution.Win32ToWPFInterop.win32clock)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.Win32)
                .SetAssemblyVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)
                .DisableRestore()
                .SetVerbosity(MSBuildVerbosity.Minimal));
        });

    Target Test => _ => _
        .After(Compile)
        .Before(Pack)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution.Snoop_Core_Tests)
                .SetConfiguration(Configuration)
                .SetVerbosity(DotNetVerbosity.Normal)
                .SetLogger("trx")
                .SetNoBuild(true)
                .SetResultsDirectory(TestResultDirectory));
        });

    Target Pack => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg", ArtifactsDirectory / "*.zip")
        .Executes(() =>
        {
            // Generate ignore files to prevent chocolatey from generating shims for them
            foreach (var launcher in CurrentBuildOutputDirectory.GlobFiles($"{ProjectName}.InjectorLauncher.*.exe"))
            {
                using var _ = File.Create(launcher + ".ignore");
            }

            NuGetTasks.NuGetPack(s => s
                .SetTargetPath(ChocolateyDirectory / $"{ProjectName}.nuspec")
                .SetVersion(NuGetVersion)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetNoPackageAnalysis(true));

            var tempDirectory = TemporaryDirectory / $"{ProjectName}{nameof(Pack)}";

            EnsureCleanDirectory(tempDirectory);

            var nupkg = ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.nupkg";

            CheckSumFiles.Add(nupkg);
            AppVeyor.Instance?.PushArtifact(nupkg);

            {
                CompressionTasks.UncompressZip(nupkg, tempDirectory);

                var outputFile = ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.zip";
                CompressionTasks.Compress(tempDirectory / "tools", outputFile, info => info.Name.Contains("chocolatey") == false && info.Name != "VERIFICATION.txt");
                CheckSumFiles.Add(outputFile);
                AppVeyor.Instance?.PushArtifact(outputFile);
            }
        });

    Target Setup => _ => _
        .DependsOn(CleanOutput)
        .DependsOn(Compile)
        .After(Pack)
        .Produces(ArtifactsDirectory / "*.msi")
        .Executes(() =>
        {
            var tempDirectory = TemporaryDirectory / $"{ProjectName}{nameof(Setup)}";

            EnsureCleanDirectory(tempDirectory);

            var candleProcess = ProcessTasks.StartProcess(CandleExecutable,
                $"{ProjectName}.wxs -ext WixUIExtension -o \"{tempDirectory / $"{ProjectName}.wixobj"}\" -dProductVersion=\"{MajorMinorPatch}\" -nologo");
            candleProcess.AssertZeroExitCode();

            var outputFile = $"{ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.msi"}";
            var lightProcess = ProcessTasks.StartProcess(LightExecutable,
                $"-out \"{outputFile}\" -b \"{CurrentBuildOutputDirectory}\" \"{tempDirectory / $"{ProjectName}.wixobj"}\" -ext WixUIExtension -dProductVersion=\"{MajorMinorPatch}\" -pdbout \"{tempDirectory / $"{ProjectName}.wixpdb"}\" -nologo -sice:ICE61");
            lightProcess.AssertZeroExitCode();

            CheckSumFiles.Add(outputFile);
            AppVeyor.Instance?.PushArtifact(outputFile);
        });

    [PublicAPI]
    Target CheckSums => _ => _
        .TriggeredBy(Pack, Setup)
        .Produces(ArtifactsDirectory / "*.sha256")
        .Executes(() =>
        {
            foreach (var item in CheckSumFiles)
            {
                var checkSum = FileHelper.SHA256CheckSum(item);
                Logger.Info(FenceOutput);
                Logger.Info($"CheckSum for \"{item}\".");
                Logger.Info($"SHA256 \"{checkSum}\".");
                var checkSumFile = item + ".sha256";
                File.WriteAllText(checkSumFile, checkSum);
                AppVeyor.Instance?.PushArtifact(checkSumFile);
                Logger.Info(FenceOutput);
            }
        });

    [Secret]
    [Parameter]
    string? SignPathAuthToken;
    [Parameter]
    string? SignPathSigningPolicySlug;
    [Parameter]
    string? SignPathProjectSlug;
    [Parameter]
    string? SignPathOrganizationId;

    Target SignArtifacts => _ => _
        .Requires(() => SignPathAuthToken)
        .OnlyWhenStatic(() => AppVeyor.Instance != null)
        .After(Setup)
        .Executes(async () =>
        {
            // ProcessTasks.StartProcess("powershell", $"./.build/SignPath.ps1 {SignPathAuthToken} {SignPathOrganizationId} {SignPathProjectSlug} {SignPathSigningPolicySlug}")
            //     .AssertWaitForExit();
            var result = await SignPathTasks.GetSigningRequestUrlViaAppVeyor(SignPathAuthToken, SignPathOrganizationId, SignPathProjectSlug, SignPathSigningPolicySlug);
            Logger.Info(result);
        });

    Target CI => _ => _
        .DependsOn(Compile, Test, Pack, Setup /*, SignArtifacts*/);
}