using System;
using System.Linq;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// Reusable component for building .NET projects (exe and library)
/// </summary>
interface IDotNetBuild : INukeBuild
{
    // Versioning via GitVersion (NUKE recommends attribute injection)
    [GitVersion(NoFetch = true)] GitVersion GitVersion => TryGetValue(() => GitVersion);
    // Parameters
    [Parameter(".NET configuration (Debug/Release)")]
    Configuration DotNetConfiguration => TryGetValue(() => DotNetConfiguration) ?? (IsLocalBuild ? Configuration.Debug : Configuration.Release);

    [Parameter("Solution file path. If not provided, the first .sln under root is used")]
    AbsolutePath DotNetSolutionPath => TryGetValue(() => DotNetSolutionPath) ?? RootDirectory.GlobFiles("**/*.sln").FirstOrDefault();

    [Parameter("Projects to operate on (globs). If empty, the solution is used")]
    string[] DotNetProjects => TryGetValue(() => DotNetProjects) ?? Array.Empty<string>();

    [Parameter("Artifacts root directory (default: build/_artifacts)")]
    AbsolutePath DotNetArtifactsRoot => TryGetValue(() => DotNetArtifactsRoot) ?? RootDirectory / "build" / "_artifacts";

    [Parameter("Output directory for dotnet artifacts (default: <ArtifactsRoot>/<VersionFolder>/tools)")]
    AbsolutePath DotNetOutput => TryGetValue(() => DotNetOutput) ?? DotNetArtifactsRoot / GetArtifactsVersionFolder() / "tools";

    [Parameter(".NET target framework override (optional)")]
    string DotNetFramework => TryGetValue(() => DotNetFramework);

    [Parameter("Runtime identifier for publish (e.g., win-x64, linux-x64) (optional)")]
    string DotNetRuntime => TryGetValue(() => DotNetRuntime);

    [Parameter("Publish single file (true/false)")]
    string DotNetPublishSingleFileRaw => TryGetValue(() => DotNetPublishSingleFileRaw);
    bool DotNetPublishSingleFile => bool.TryParse(DotNetPublishSingleFileRaw, out var v1) && v1;

    [Parameter("Self-contained publish (true/false)")]
    string DotNetSelfContainedRaw => TryGetValue(() => DotNetSelfContainedRaw);
    bool DotNetSelfContained => bool.TryParse(DotNetSelfContainedRaw, out var v2) && v2;

    [Parameter("Version suffix (e.g., 1.2.3 or ci-1234) for pack/publish and artifact folder name")]
    string DotNetVersionSuffix => TryGetValue(() => DotNetVersionSuffix);

    // Internal resolver for projects
    private string[] GetProjectSet()
        => (DotNetProjects != null && DotNetProjects.Length > 0)
            ? DotNetProjects.SelectMany(p => RootDirectory.GlobFiles(p).Select(x => (string)x)).ToArray()
            : (DotNetSolutionPath != null ? new[] { (string)DotNetSolutionPath } : Array.Empty<string>());

    // Helpers
    private string GetArtifactsVersionFolder()
    {
        var name = DotNetVersionSuffix;
        if (string.IsNullOrWhiteSpace(name) && GitVersion != null)
            name = GitVersion.NuGetVersionV2 ?? GitVersion.SemVer ?? GitVersion.InformationalVersion;
        if (string.IsNullOrWhiteSpace(name))
            name = IsLocalBuild ? "local" : "ci";
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name;
    }

    // Targets
    Target CleanDotNet => _ => _
        .Description("Clean .NET output directory")
        .Executes(() =>
        {
            Directory.CreateDirectory(DotNetArtifactsRoot);
            Directory.CreateDirectory(DotNetOutput);
        });

    Target RestoreDotNet => _ => _
        .Description("dotnet restore for solution/projects")
        .Executes(() =>
        {
            var items = GetProjectSet();
            if (items.Length == 0)
                Serilog.Log.Warning("No solution or projects found for RestoreDotNet.");

            items.ForEach(item =>
            {
                DotNetRestore(s => s
                    .SetProjectFile(item)
                );
            });
        });

    Target BuildDotNet => _ => _
        .Description("dotnet build for solution/projects")
        .DependsOn(RestoreDotNet)
        .Executes(() =>
        {
            var items = GetProjectSet();
            if (items.Length == 0)
                Serilog.Log.Warning("No solution or projects found for BuildDotNet.");

            items.ForEach(item =>
            {
                DotNetBuild(s =>
                {
                    var cfg = s
                        .SetProjectFile(item)
                        .SetConfiguration(DotNetConfiguration)
                        .EnableNoRestore();
                    if (!string.IsNullOrEmpty(DotNetFramework)) cfg = cfg.SetFramework(DotNetFramework);
                    // Use explicit suffix if provided; otherwise prefer GitVersion full version when building packages later.
                    if (!string.IsNullOrEmpty(DotNetVersionSuffix)) cfg = cfg.SetVersionSuffix(DotNetVersionSuffix);
                    else if (GitVersion != null) cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);
                    return cfg;
                });
            });
        });

    Target TestDotNet => _ => _
        .Description("dotnet test for solution/projects")
        .DependsOn(BuildDotNet)
        .Executes(() =>
        {
            var items = GetProjectSet();
            items.ForEach(item =>
            {
                DotNetTest(s => s
                    .SetProjectFile(item)
                    .SetConfiguration(DotNetConfiguration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                );
            });
        });

    Target PackDotNet => _ => _
        .Description("dotnet pack for solution/projects")
        .DependsOn(BuildDotNet)
        .Executes(() =>
        {
            var items = GetProjectSet();
            items.ForEach(item =>
            {
                DotNetPack(s =>
                {
                    var cfg = s
                        .SetProject(item)
                        .SetConfiguration(DotNetConfiguration)
                        .SetOutputDirectory(DotNetOutput)
                        .EnableNoBuild()
                        .EnableNoRestore();
                    if (!string.IsNullOrEmpty(DotNetVersionSuffix)) cfg = cfg.SetVersionSuffix(DotNetVersionSuffix);
                    else if (GitVersion != null) cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);
                    return cfg;
                });
            });
        });

    Target PublishDotNet => _ => _
        .Description("dotnet publish (exe/lib) for projects")
        .DependsOn(BuildDotNet)
        .Executes(() =>
        {
            var projects = DotNetProjects != null && DotNetProjects.Length > 0 ? GetProjectSet() : Array.Empty<string>();
            if (projects.Length == 0)
            {
                Serilog.Log.Information("No specific projects provided for publish. Skipping PublishDotNet.");
                return;
            }

            projects.ForEach(project =>
            {
                DotNetPublish(s =>
                {
                    var cfg = s
                        .SetProject(project)
                        .SetConfiguration(DotNetConfiguration)
                        .SetOutput(DotNetOutput)
                        .EnableNoBuild()
                        .EnableNoRestore();
                    if (!string.IsNullOrEmpty(DotNetRuntime)) cfg = cfg.SetRuntime(DotNetRuntime);
                    if (!string.IsNullOrEmpty(DotNetFramework)) cfg = cfg.SetFramework(DotNetFramework);
                    if (DotNetPublishSingleFile) cfg = cfg.EnablePublishSingleFile();
                    if (DotNetSelfContained) cfg = cfg.EnableSelfContained();
                    if (!string.IsNullOrEmpty(DotNetVersionSuffix)) cfg = cfg.SetVersionSuffix(DotNetVersionSuffix);
                    else if (GitVersion != null) cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);
                    return cfg;
                });
            });
        });
}
