using System;
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
/// Build preparation component implementing multi-stage injection workflow.
/// Phase 1: Populate cache (safe anytime)
/// Phase 2: Multi-stage injection (build-time with stage-specific cleanup)
///
/// Stages:
/// - PreTest: Test-specific dependencies (before validation)
/// - PreBuild: Core Unity dependencies (current behavior)
/// - PostBuild: Runtime test dependencies (after Unity build)
/// - PreNativeBuild: Native platform dependencies (iOS/Android)
/// - PostNativeBuild: Final packaging modifications
/// </summary>
partial class Build
{
    [Parameter("Path to multi-stage preparation config (v2.0 - cache population)")]
    public AbsolutePath MultiStagePreparationConfig = RootDirectory / "build" / "configs" / "preparation" / "multi-stage-preparation.json";

    [Parameter("Path to multi-stage injection config (v2.0 - injection stages with operations)")]
    public AbsolutePath MultiStageConfig = RootDirectory / "build" / "configs" / "preparation" / "multi-stage-injection.json";

    [Parameter("Enable multi-stage injection (default: auto-detect from config)")]
    public bool? UseMultiStage = null;

    [Parameter("Injection stage to execute (preTest, preBuild, postBuild, preNativeBuild, postNativeBuild)")]
    public string InjectionStage = null;

    AbsolutePath PreparationToolProject => RootDirectory / "packages" / "scoped-6571" /
        "com.contractwork.sangocard.build" / "dotnet~" / "tool" / "SangoCard.Build.Tool" /
        "SangoCard.Build.Tool.csproj";

    AbsolutePath ClientProject => RootDirectory / "projects" / "client";

    AbsolutePath CodeQualityProject => RootDirectory / "projects" / "code-quality";

    /// <summary>
    /// Phase 1: Populate preparation cache from code-quality project (safe anytime).
    /// This can be run independently without modifying the client project.
    /// Uses v2.0 multi-stage-preparation.json config.
    /// </summary>
    Target PrepareCache => _ => _
        .Description("Phase 1: Populate preparation cache using v2.0 multi-stage config")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Phase 1: Populating Cache (V2.0) ===");
            Serilog.Log.Information("Source: {Source}", CodeQualityProject);
            Serilog.Log.Information("Config: {Config}", MultiStagePreparationConfig);

            var relativeSource = RootDirectory.GetRelativePathTo(CodeQualityProject);
            var relativeConfig = RootDirectory.GetRelativePathTo(MultiStagePreparationConfig);

            var process = StartProcess(
                "dotnet",
                $"run --project \"{PreparationToolProject}\" -- cache populate --source {relativeSource} --config {relativeConfig}",
                workingDirectory: RootDirectory
            );
            process.AssertZeroExitCode();

            Serilog.Log.Information("✅ Cache population complete");
        });

    // ========================================
    // Multi-Stage Injection Targets
    // ========================================

    /// <summary>
    /// Inject pre-test dependencies (Stage 1).
    /// Test-specific dependencies injected before TestPreBuild.
    /// </summary>
    Target InjectPreTest => _ => _
        .Description("Stage 1: Inject test-specific dependencies")
        .DependsOn(PrepareCache)
        .OnlyWhenDynamic(() => IsStageEnabled("preTest"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Stage 1: Pre-Test Injection ===");
            RunInjectionStage("preTest");
        });

    /// <summary>
    /// Inject pre-build dependencies (Stage 2).
    /// Core Unity build dependencies - equivalent to legacy PrepareClient.
    /// </summary>
    Target InjectPreBuild => _ => _
        .Description("Stage 2: Inject core build dependencies")
        .DependsOn(PrepareCache)
        .OnlyWhenDynamic(() => IsStageEnabled("preBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Stage 2: Pre-Build Injection ===");

            // R-BLD-060: Reset client before injection
            Serilog.Log.Information("Resetting client project (git reset --hard)...");
            Git("reset --hard", workingDirectory: ClientProject);
            Serilog.Log.Information("✅ Client reset complete");

            RunInjectionStage("preBuild");
        });

    /// <summary>
    /// Inject post-build dependencies (Stage 3).
    /// Runtime testing dependencies injected after Unity build.
    /// </summary>
    Target InjectPostBuild => _ => _
        .Description("Stage 3: Inject post-build test dependencies")
        .OnlyWhenDynamic(() => IsStageEnabled("postBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Stage 3: Post-Build Injection ===");
            RunInjectionStage("postBuild");
        });

    /// <summary>
    /// Inject pre-native-build dependencies (Stage 4).
    /// Platform-specific native dependencies (iOS/Android).
    /// </summary>
    Target InjectPreNativeBuild => _ => _
        .Description("Stage 4: Inject platform-specific native dependencies")
        .OnlyWhenDynamic(() => IsStageEnabled("preNativeBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Stage 4: Pre-Native-Build Injection ===");
            var platform = ((IUnityBuild)this).UnityBuildTarget?.ToString() ?? "StandaloneWindows64";
            RunInjectionStage("preNativeBuild", platform);
        });

    /// <summary>
    /// Inject post-native-build dependencies (Stage 5).
    /// Final packaging modifications.
    /// </summary>
    Target InjectPostNativeBuild => _ => _
        .Description("Stage 5: Inject packaging dependencies")
        .OnlyWhenDynamic(() => IsStageEnabled("postNativeBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Stage 5: Post-Native-Build Injection ===");
            RunInjectionStage("postNativeBuild");
        });

    // ========================================
    // Cleanup Targets
    // ========================================

    /// <summary>
    /// Cleanup pre-test injection (after pre-build tests).
    /// </summary>
    Target CleanupPreTest => _ => _
        .Description("Cleanup pre-test injection")
        .AssuredAfterFailure()
        .After(((ITestBuild)this).TestPreBuild)
        .OnlyWhenDynamic(() => ShouldCleanupStage("preTest"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Cleanup: Pre-Test Stage ===");
            CleanupInjectionStage("preTest");
        });

    /// <summary>
    /// Cleanup post-build injection (after post-build tests).
    /// </summary>
    Target CleanupPostBuild => _ => _
        .Description("Cleanup post-build injection")
        .AssuredAfterFailure()
        .After(((ITestBuild)this).TestPostBuild)
        .OnlyWhenDynamic(() => ShouldCleanupStage("postBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Cleanup: Post-Build Stage ===");
            CleanupInjectionStage("postBuild");
        });

    /// <summary>
    /// Cleanup pre-native-build injection (after native build).
    /// </summary>
    Target CleanupPreNativeBuild => _ => _
        .Description("Cleanup pre-native-build injection")
        .AssuredAfterFailure()
        .OnlyWhenDynamic(() => ShouldCleanupStage("preNativeBuild"))
        .Executes(() =>
        {
            Serilog.Log.Information("=== Cleanup: Pre-Native-Build Stage ===");
            CleanupInjectionStage("preNativeBuild");
        });

    // ========================================
    // Legacy Compatibility
    // ========================================

    /// <summary>
    /// Phase 2: Inject preparation into Unity client project (build-time only).
    /// ⚠️ DEPRECATED: Use BuildWithMultiStage instead.
    /// LEGACY: Maintained for backward compatibility. Direct injection without multi-stage.
    /// IMPORTANT: This performs git reset --hard before injection per R-BLD-060.
    ///
    /// DEPRECATION NOTICE:
    /// This target uses V1.0 configuration format which is deprecated.
    /// Please migrate to V2.0 multi-stage format and use BuildWithMultiStage target.
    /// See: build/configs/preparation/DEPRECATION.md
    /// </summary>
    Target PrepareClient => _ => _
        .Description("⚠️ DEPRECATED: Use BuildWithMultiStage - Phase 2: Inject preparation into client (V1.0 legacy)")
        .DependsOn(PrepareCache)
        .Executes(() =>
        {
            Serilog.Log.Warning("⚠️  DEPRECATION WARNING: PrepareClient target uses V1.0 configuration format");
            Serilog.Log.Warning("   Please migrate to V2.0 multi-stage format and use BuildWithMultiStage target");
            Serilog.Log.Warning("   See: docs/_inbox/v1-config-deprecation.md");
            Serilog.Log.Warning("");

            Serilog.Log.Information("=== Phase 2: Injecting to Client (Legacy V1.0 Mode) ===");

            // R-BLD-060: Reset client before injection
            Serilog.Log.Information("Resetting client project (git reset --hard)...");
            Git("reset --hard", workingDirectory: ClientProject);
            Serilog.Log.Information("✅ Client reset complete");

            // Inject from cache (legacy mode - no --stage parameter)
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

            Serilog.Log.Information("✅ Client preparation complete (legacy mode)");
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
    /// Cleanup target that restores client after successful build.
    /// Uses AssuredAfterFailure to guarantee execution regardless of build outcome.
    /// Only performs cleanup if build was successful - preserves injected state on failure for debugging.
    /// </summary>
    Target CleanupAfterBuild => _ => _
        .Description("Cleanup client project after build (success only)")
        .AssuredAfterFailure()
        .After(((IUnityBuild)this).BuildUnity)
        .OnlyWhenDynamic(() => IsServerBuild || SucceededTargets.Contains(((IUnityBuild)this).BuildUnity))
        .Executes(() =>
        {
            if (SucceededTargets.Contains(((IUnityBuild)this).BuildUnity))
            {
                Serilog.Log.Information("=== Cleanup After Successful Build ===");
                Serilog.Log.Information("Build succeeded - restoring client to clean state...");
                Git("reset --hard", workingDirectory: ClientProject);
                Serilog.Log.Information("✅ Client restored to clean state");
            }
            else
            {
                Serilog.Log.Warning("=== Build Failed - Preserving Injected State ===");
                Serilog.Log.Warning("Client project NOT restored to allow debugging");
                Serilog.Log.Warning("Run 'task build:prepare:restore' to manually clean when done");
            }
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
    /// ⚠️ DEPRECATED: Use BuildWithMultiStage instead.
    ///
    /// Executes: PrepareCache → PrepareClient → BuildUnity → CleanupAfterBuild
    /// CleanupAfterBuild uses AssuredAfterFailure to guarantee cleanup logic runs.
    /// On success: Client is restored to clean state (git reset --hard)
    /// On failure: Client state preserved for debugging (manual cleanup needed)
    ///
    /// DEPRECATION NOTICE:
    /// This target uses V1.0 configuration format which is deprecated.
    /// Please migrate to V2.0 multi-stage format and use BuildWithMultiStage target.
    /// See: build/configs/preparation/DEPRECATION.md
    /// </summary>
    Target BuildUnityWithPreparation => _ => _
        .Description("⚠️ DEPRECATED: Use BuildWithMultiStage - Full Unity build with V1.0 preparation")
        .DependsOn(PrepareClient)
        .Triggers(((IUnityBuild)this).BuildUnity)
        .Triggers(CleanupAfterBuild)
        .Executes(() =>
        {
            Serilog.Log.Warning("⚠️  DEPRECATION WARNING: BuildUnityWithPreparation uses V1.0 configuration");
            Serilog.Log.Warning("   Please use BuildWithMultiStage target with V2.0 multi-stage format");
            Serilog.Log.Warning("   See: docs/_inbox/v1-config-deprecation.md");
            Serilog.Log.Warning("");

            Serilog.Log.Information("✅ Unity build with preparation workflow complete");
        });

    /// <summary>
    /// Full Unity build with multi-stage injection workflow.
    /// Executes: PrepareCache (v1) → InjectPreBuild (v2) → BuildUnity → CleanupAfterBuild
    /// Uses multi-stage-preparation.json for preBuild stage execution.
    /// </summary>
    Target BuildWithMultiStage => _ => _
        .Description("Full Unity build with multi-stage injection (v2.0 config)")
        .DependsOn(PrepareCache)
        .DependsOn(InjectPreBuild)
        .Triggers(((IUnityBuild)this).BuildUnity)
        .Triggers(CleanupAfterBuild)
        .Executes(() =>
        {
            Serilog.Log.Information("✅ Multi-stage build workflow complete");

            if (SucceededTargets.Contains(((IUnityBuild)this).BuildUnity))
            {
                Serilog.Log.Information("✅ Build succeeded");
            }
            else
            {
                Serilog.Log.Warning("⚠️ Build failed - check logs for details");
            }
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

    // ========================================
    // Helper Methods
    // ========================================

    /// <summary>
    /// Check if a specific injection stage is enabled in configuration.
    /// </summary>
    bool IsStageEnabled(string stage)
    {
        // If stage is explicitly specified, only run that stage
        if (!string.IsNullOrEmpty(InjectionStage))
        {
            return stage.Equals(InjectionStage, StringComparison.OrdinalIgnoreCase);
        }

        // TODO: Parse config to check if stage is enabled
        // For now, preBuild is always enabled (legacy behavior)
        if (stage == "preBuild")
            return true;

        Serilog.Log.Debug("Stage {Stage} not enabled", stage);
        return false;
    }

    /// <summary>
    /// Check if a specific injection stage should be cleaned up.
    /// </summary>
    bool ShouldCleanupStage(string stage)
    {
        // Only cleanup if stage was injected and succeeded
        var injectionTarget = stage switch
        {
            "preTest" => InjectPreTest,
            "postBuild" => InjectPostBuild,
            "preNativeBuild" => InjectPreNativeBuild,
            _ => null
        };

        if (injectionTarget == null)
            return false;

        // Check if target was executed and succeeded
        bool wasInjected = ScheduledTargets.Contains(injectionTarget) || SucceededTargets.Contains(injectionTarget);
        bool succeeded = SucceededTargets.Contains(injectionTarget);

        Serilog.Log.Debug("Stage {Stage} cleanup check: injected={Injected}, succeeded={Succeeded}",
            stage, wasInjected, succeeded);

        return wasInjected && succeeded;
    }

    /// <summary>
    /// Run injection for a specific stage.
    /// </summary>
    void RunInjectionStage(string stage, string platform = null)
    {
        Serilog.Log.Information("Injecting {Stage} stage...", stage);

        // Use multi-stage config for v2.0 workflow
        var configPath = MultiStageConfig;
        Serilog.Log.Information("Config: {Config}", configPath);
        Serilog.Log.Information("Target: projects/client/");

        var relativeConfig = RootDirectory.GetRelativePathTo(configPath);
        var relativeClient = RootDirectory.GetRelativePathTo(ClientProject);

        var args = $"run --project \"{PreparationToolProject}\" -- prepare inject --config {relativeConfig} --target {relativeClient} --stage {stage}";

        if (!string.IsNullOrEmpty(platform))
        {
            args += $" --platform {platform}";
        }

        args += " --verbose";

        var process = StartProcess("dotnet", args, workingDirectory: RootDirectory);
        process.AssertZeroExitCode();

        Serilog.Log.Information("✅ {Stage} injection complete", stage);
    }

    /// <summary>
    /// Cleanup injection for a specific stage.
    /// </summary>
    void CleanupInjectionStage(string stage)
    {
        Serilog.Log.Information("Cleaning up {Stage} stage...", stage);

        // For now, use git reset for cleanup
        // TODO: Implement stage-specific cleanup in preparation tool
        Git("reset --hard", workingDirectory: ClientProject);

        Serilog.Log.Information("✅ {Stage} cleanup complete", stage);
    }
}
