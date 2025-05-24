using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AppVeyor;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Tools.SignPath;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
// ReSharper disable AllUnderscoreLocalParameterName

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

        Serilog.Log.Information("IsLocalBuild           : {0}", IsLocalBuild.ToString());

        Serilog.Log.Information("Informational   Version: {0}", InformationalVersion);
        Serilog.Log.Information("SemVer          Version: {0}", SemVer);
        Serilog.Log.Information("AssemblySemVer  Version: {0}", AssemblySemVer);
        Serilog.Log.Information("MajorMinorPatch Version: {0}", MajorMinorPatch);
        Serilog.Log.Information("NuGet           Version: {0}", NuGetVersion);
    }

    string ProjectName = "Snoop";

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution = null!;
    [Solution(GenerateProjects = true)] readonly TestHarnessSolution TestHarnessSolution = null!;

    [GitRepository] GitRepository? GitRepository;
    [GitVersion(Framework = "net6.0", NoFetch = true, NoCache = true)]
    readonly GitVersion? GitVersion;

    string AssemblySemVer => GitVersion?.AssemblySemVer ?? "1.0.0";
    string SemVer => GitVersion?.SemVer ?? "1.0.0";
    string InformationalVersion => GitVersion?.InformationalVersion ?? "1.0.0";
    string NuGetVersion => GitVersion?.NuGetVersion ?? "1.0.0";
    string MajorMinorPatch => GitVersion?.MajorMinorPatch ?? "1.0.0";

    [CI]
    readonly GitHubActions? GitHubActions;

    readonly List<string> CheckSumFiles = new();

    AbsolutePath BuildBinDirectory => RootDirectory / "bin";

    AbsolutePath CurrentBuildOutputDirectory => BuildBinDirectory / Configuration;

    [Parameter]
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "artifacts";

    AbsolutePath OutputDirectory => RootDirectory / "output";

    AbsolutePath ChocolateyDirectory => RootDirectory / "chocolatey";

    AbsolutePath TestResultDirectory => OutputDirectory / "test-results";

    readonly string WixUIExtension = "WixToolset.UI.wixext/6.0.0";

    readonly string FenceOutput = "".PadLeft(30, '#');

    Target CleanOutput => _ => _
        .Executes(() =>
        {
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetToolRestore();

            ProcessTasks.StartProcess("dotnet", $"wix extension add {WixUIExtension}")
                .AssertZeroExitCode();

            DotNetRestore(s => s
                .SetProjectFile(Solution));

            DotNetRestore(s => s
                .SetProjectFile(TestHarnessSolution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(XamlStyler)
        .Executes(() =>
        {
            string toolsPath = string.Empty;
            try
            {
                toolsPath = MSBuildToolPathResolver.Resolve(MSBuildVersion.VS2019, MSBuildPlatform.x64);
            }
            catch
            {
                // ignored
            }

            if (string.IsNullOrEmpty(toolsPath))
            {
                foreach (var edition in new[] { "Enterprise", "Professional", "Community", "BuildTools", "Preview" })
                {
                    var toolPath = Path.Combine(
                        EnvironmentInfo.SpecialFolder(SpecialFolders.ProgramFiles).NotNull("path1 != null"),
                        $@"Microsoft Visual Studio\2022\{edition}\MSBuild\Current\Bin\amd64\msbuild.exe");

                    if (File.Exists(toolPath))
                    {
                        toolsPath = toolPath;
                        break;
                    }
                }
            }

            MSBuild(s => s
                .SetProjectFile(Solution.Snoop_GenericInjector)
                .SetProcessToolPath(toolsPath)
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
                .SetVerbosity(DotNetVerbosity.minimal));
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
                .EnableNoRestore()
                .SetVerbosity(DotNetVerbosity.minimal));

            MSBuild(s => s
                .SetProjectFile(TestHarnessSolution.Win32ToWPFInterop.win32clock)
                .SetConfiguration(Configuration)
                .SetTargetPlatform(MSBuildTargetPlatform.Win32)
                .SetAssemblyVersion(AssemblySemVer)
                .SetInformationalVersion(InformationalVersion)
                .DisableRestore()
                .SetVerbosity(MSBuildVerbosity.Minimal));
        });

    Target XamlStyler => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNet($"xstyler --recursive --directory \"{RootDirectory}\"");
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
                .SetVerbosity(DotNetVerbosity.normal)
                .AddLoggers("trx")
                .EnableNoBuild()
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
                .EnableNoPackageAnalysis());

            var tempDirectory = TemporaryDirectory / $"{ProjectName}{nameof(Pack)}";

            tempDirectory.CreateOrCleanDirectory();

            var nupkg = ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.nupkg";

            CheckSumFiles.Add(nupkg);
            AppVeyor.Instance?.PushArtifact(nupkg);

            {
                nupkg.UnZipTo(tempDirectory);

                var outputFile = ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.zip";
                (tempDirectory / "tools").CompressTo(outputFile, info => info.Name.Contains("chocolatey") == false && info.Name != "VERIFICATION.txt");
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
            var outputFile = $"{ArtifactsDirectory / $"{ProjectName}.{NuGetVersion}.msi"}";
            ProcessTasks.StartProcess("dotnet", $"wix build -bindpath \"{CurrentBuildOutputDirectory}\" -define ProductVersion=\"{MajorMinorPatch}\" -ext {WixUIExtension} -o \"{outputFile}\" -nologo {ProjectName}.wxs")
                .AssertZeroExitCode();
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
                Serilog.Log.Information(FenceOutput);
                Serilog.Log.Information($"CheckSum for \"{item}\".");
                Serilog.Log.Information($"SHA256 \"{checkSum}\".");
                var checkSumFile = item + ".sha256";
                File.WriteAllText(checkSumFile, checkSum);
                AppVeyor.Instance?.PushArtifact(checkSumFile);
                Serilog.Log.Information(FenceOutput);
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
        .OnlyWhenStatic(ShouldSign)
        .After(Setup)
        .Executes(async () =>
        {
            {
                var outputFile = ArtifactsDirectory / $"{ProjectName}.Sign.{NuGetVersion}.zip";
                ArtifactsDirectory.CompressTo(outputFile);
                CheckSumFiles.Add(outputFile);
                AppVeyor.Instance?.PushArtifact(outputFile);
            }

            // ProcessTasks.StartProcess("powershell", $"./.build/SignPath.ps1 {SignPathAuthToken} {SignPathOrganizationId} {SignPathProjectSlug} {SignPathSigningPolicySlug}")
            //     .AssertWaitForExit();

            var result = await SignPathTasks2.GetSigningRequestUrlViaAppVeyor(SignPathAuthToken!, SignPathOrganizationId!, SignPathProjectSlug!, SignPathSigningPolicySlug!);
            Serilog.Log.Information(result);
        });

    bool ShouldSign()
    {
        if (AppVeyor.Instance is null
            || GitRepository is null
            || GitVersion is null
            || string.IsNullOrEmpty(SignPathAuthToken))
        {
            return false;
        }

        return GitRepository.IsOnMainOrMasterBranch()
               // Pre-Release or not?
               || GitVersion.NuGetVersion.Contains('-') == false;
    }

    // ReSharper disable once InconsistentNaming
    Target CI => _ => _
        .DependsOn(Compile, Test, Pack, Setup, SignArtifacts);
}