using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;

/// <summary>
/// Main build orchestration class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This class uses partial class pattern for interface segregation.
/// - Build.cs: Contains base NukeBuild inheritance only
/// - Build.UnityBuild.cs: Contains IUnityBuild interface implementation
/// Each interface is implemented in its own partial class file for better organization.
/// </summary>
partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    // Unified versioning across components (DotNet, Report, Unity)
    // If not provided, falls back to GitVersion when available, otherwise "local".
    [Nuke.Common.Tools.GitVersion.GitVersion(NoFetch = true)]
    public Nuke.Common.Tools.GitVersion.GitVersion GitVersion { get; } = null!;

    [Parameter("Artifacts version suffix for packages and folders (e.g., 1.2.3 or ci-1234)")]
    public string? ArtifactsVersionSuffix { get; set; }

    public string EffectiveVersion
        => !string.IsNullOrWhiteSpace(ArtifactsVersionSuffix)
            ? ArtifactsVersionSuffix!
            : (GitVersion?.NuGetVersionV2 ?? GitVersion?.SemVer ?? GitVersion?.InformationalVersion ?? (IsLocalBuild ? "local" : "ci"));

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });

}
