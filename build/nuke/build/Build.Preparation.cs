using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tooling.ProcessTasks;

/// <summary>
/// Build preparation component implementing two-phase workflow.
/// Phase 1: Populate cache (safe anytime)
/// Phase 2: Inject to client (build-time only with git reset)
/// </summary>
partial class Build
{
    [Parameter("Path to preparation config")]
    public AbsolutePath PreparationConfig = RootDirectory / "build" / "preparation" / "configs" / "preparation.json";

    AbsolutePath PreparationToolProject => RootDirectory / "packages" / "scoped-6571" /
        "com.contractwork.sangocard.build" / "dotnet~" / "tool" / "SangoCard.Build.Tool" /
        "SangoCard.Build.Tool.csproj";

    AbsolutePath ClientProject => RootDirectory / "projects" / "client";

    AbsolutePath CodeQualityProject => RootDirectory / "projects" / "code-quality";

    /// <summary>
    /// Phase 1: Populate preparation cache from code-quality project (safe anytime).
    /// This can be run independently without modifying the client project.
    /// </summary>
    Target PrepareCache => _ => _
        .Description("Phase 1: Populate preparation cache (safe anytime, no client modification)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Phase 1: Populating Cache ===");
            Serilog.Log.Information("Source: {Source}", CodeQualityProject);
            Serilog.Log.Information("Config: {Config}", PreparationConfig);

            var relativeSource = RootDirectory.GetRelativePathTo(CodeQualityProject);
            var relativeConfig = RootDirectory.GetRelativePathTo(PreparationConfig);

            var process = StartProcess(
                "dotnet",
                $"run --project \"{PreparationToolProject}\" -- cache populate --source {relativeSource} --config {relativeConfig}",
                workingDirectory: RootDirectory
            );
            process.AssertZeroExitCode();

            Serilog.Log.Information("✅ Cache population complete");
        });

    /// <summary>
    /// Phase 2: Inject preparation into Unity client project (build-time only).
    /// IMPORTANT: This performs git reset --hard before injection per R-BLD-060.
    /// </summary>
    Target PrepareClient => _ => _
        .Description("Phase 2: Inject preparation into client (build-time only, performs git reset)")
        .DependsOn(PrepareCache)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Phase 2: Injecting to Client ===");

            // R-BLD-060: Reset client before injection
            Serilog.Log.Information("Resetting client project (git reset --hard)...");
            Git("reset --hard", workingDirectory: ClientProject);
            Serilog.Log.Information("✅ Client reset complete");

            // Inject from cache
            Serilog.Log.Information("Injecting preparation...");
            Serilog.Log.Information("Config: {Config}", PreparationConfig);
            Serilog.Log.Information("Target: projects/client/");

            var relativeConfig = RootDirectory.GetRelativePathTo(PreparationConfig);
            var relativeClient = RootDirectory.GetRelativePathTo(ClientProject);

            var process = StartProcess(
                "dotnet",
                $"run --project \"{PreparationToolProject}\" -- prepare inject --config {relativeConfig} --target {relativeClient} --verbose",
                workingDirectory: RootDirectory
            );
            process.AssertZeroExitCode();

            Serilog.Log.Information("✅ Client preparation complete");
        });

    /// <summary>
    /// Restore Unity client project to clean state (git reset --hard).
    /// Use this manually after failed builds or to clean up injected files.
    /// Note: Successful builds automatically restore the client.
    /// </summary>
    Target RestoreClient => _ => _
        .Description("Restore Unity client project to clean state (git reset)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Restoring Client Project ===");
            Serilog.Log.Information("Performing git reset --hard...");

            Git("reset --hard", workingDirectory: ClientProject);

            Serilog.Log.Information("✅ Client restored to clean state");
            Serilog.Log.Information("Note: Run 'task build:prepare:restore' to restore client manually");
        });

    /// <summary>
    /// Validate preparation configuration without executing.
    /// </summary>
    Target ValidatePreparation => _ => _
        .Description("Validate preparation configuration")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Validating Preparation Config ===");
            Serilog.Log.Information("Config: {Config}", PreparationConfig);

            var relativeConfig = RootDirectory.GetRelativePathTo(PreparationConfig);

            var process = StartProcess(
                "dotnet",
                $"run --project \"{PreparationToolProject}\" -- validate --file {relativeConfig} --level Full",
                workingDirectory: RootDirectory
            );
            process.AssertZeroExitCode();

            Serilog.Log.Information("✅ Validation complete");
        });

    /// <summary>
    /// Full Unity build workflow with preparation.
    /// Executes: PrepareCache → PrepareClient → BuildUnity → RestoreClient (on success only)
    /// Note: Client is NOT restored on build failure to allow debugging of injected state.
    /// </summary>
    Target BuildUnityWithPreparation => _ => _
        .Description("Full Unity build with two-phase preparation")
        .DependsOn(PrepareClient)
        .DependsOn(((IUnityBuild)this).BuildUnity)
        .TriggeredBy(((IUnityBuild)this).BuildUnity)
        .Executes(() =>
        {
            Serilog.Log.Information("✅ Unity build with preparation complete");

            // R-BLD-060: Restore client to clean state after successful build
            // This only runs if BuildUnity succeeds - failures preserve state for debugging
            Serilog.Log.Information("Restoring client project to clean state...");
            Git("reset --hard", workingDirectory: ClientProject);
            Serilog.Log.Information("✅ Client restored to clean state");
        });

    /// <summary>
    /// Dry-run of preparation injection to see what would be changed.
    /// </summary>
    Target DryRunPreparation => _ => _
        .Description("Dry-run preparation injection (shows changes without applying)")
        .DependsOn(PrepareCache)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Dry-Run: Preparation Injection ===");
            Serilog.Log.Information("Config: {Config}", PreparationConfig);

            var relativeConfig = RootDirectory.GetRelativePathTo(PreparationConfig);
            var relativeClient = RootDirectory.GetRelativePathTo(ClientProject);

            var process = StartProcess(
                "dotnet",
                $"run --project \"{PreparationToolProject}\" -- prepare inject --config {relativeConfig} --target {relativeClient} --verbose --dry-run",
                workingDirectory: RootDirectory
            );
            process.AssertZeroExitCode();

            Serilog.Log.Information("✅ Dry-run complete (no files modified)");
        });
}
