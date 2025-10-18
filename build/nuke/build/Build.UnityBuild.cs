using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// Unity build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the IUnityBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.UnityBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : IUnityBuild
{
    // Use RepoRoot from IReportBuild (available via Build.ReportBuild.cs) for consistent path resolution
    public AbsolutePath UnityProjectPath => ((IReportBuild)this).RepoRoot / "projects" / "client";

    // Interface implementation is provided by IUnityBuild default interface members
    // Override default members here if custom implementation is needed

    // Unify Unity build version with EffectiveVersion so client and report share the same version folder
    public string UnityBuildVersion => EffectiveVersion;

    // Build Unity client and generate report packages into build/_artifacts/<EffectiveVersion>/report
    Target BuildClient => _ => _
        .Description("Build Unity client and generate report artifacts (version-synced)")
        .DependsOn(((IUnityBuild)this).BuildUnity, ((IReportBuild)this).PackReport, ((IReportBuild)this).GenerateReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Client build and report artifacts complete. Version: {Version}", EffectiveVersion);
            var artifactsRoot = ((IReportBuild)this).RepoRoot / "build" / "_artifacts" / EffectiveVersion;
            Serilog.Log.Information("Artifacts root: {Path}", artifactsRoot);
        });
}
