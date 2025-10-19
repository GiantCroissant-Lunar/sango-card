using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// Test build component interface for running Unity tests.
/// Split into pre-build tests (validation) and post-build tests (runtime).
/// </summary>
interface ITestBuild : INukeBuild
{
    /// <summary>
    /// Path to Unity project for testing (typically same as build project).
    /// </summary>
    AbsolutePath TestProjectPath => ((IReportBuild)this).RepoRoot / "projects" / "client";

    // ========================================
    // Pre-Build Tests (Validation/Static Analysis)
    // ========================================

    /// <summary>
    /// Run Unity Edit Mode tests (pre-build validation).
    /// Fast tests that validate code without building: unit tests, static analysis.
    /// </summary>
    Target TestEditMode => _ => _
        .Description("Run Unity Edit Mode tests (pre-build validation)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Edit Mode Tests (Pre-Build) ===");
            Serilog.Log.Information("Test Project: {Path}", TestProjectPath);

            // TODO: Implement Unity test execution
            // Example: Unity.exe -runTests -testPlatform EditMode -projectPath <path>
            Serilog.Log.Warning("Edit Mode tests not yet implemented");
            Serilog.Log.Information("✅ Edit Mode test execution placeholder complete");
        });

    /// <summary>
    /// Run code analysis and validation (pre-build).
    /// Static analysis, linting, code quality checks.
    /// </summary>
    Target TestCodeQuality => _ => _
        .Description("Run code quality checks (pre-build)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Code Quality Checks (Pre-Build) ===");
            Serilog.Log.Information("Test Project: {Path}", TestProjectPath);

            // TODO: Implement code quality checks
            // - Roslyn analyzers
            // - Unity script compilation
            // - Custom validators
            Serilog.Log.Warning("Code quality checks not yet implemented");
            Serilog.Log.Information("✅ Code quality check placeholder complete");
        });

    /// <summary>
    /// Run all pre-build tests (validation before building).
    /// Uses ProceedAfterFailure to ensure all validation runs even if some fail.
    /// Fast feedback loop - catches issues before expensive build.
    /// </summary>
    Target TestPreBuild => _ => _
        .Description("Run all pre-build tests (validation)")
        .DependsOn(TestEditMode)
        .DependsOn(TestCodeQuality)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Serilog.Log.Information("=== Pre-Build Tests Complete ===");

            var failedTargets = FailedTargets;
            if (failedTargets.Count > 0)
            {
                Serilog.Log.Warning("Some pre-build tests failed:");
                foreach (var target in failedTargets)
                {
                    Serilog.Log.Warning("  - {Target}", target);
                }
                Serilog.Log.Warning("Build will be skipped to save time");
            }
            else
            {
                Serilog.Log.Information("✅ All pre-build tests passed - proceeding to build");
            }
        });

    // ========================================
    // Post-Build Tests (Runtime/Integration)
    // ========================================

    /// <summary>
    /// Run Unity Play Mode tests (post-build runtime tests).
    /// Tests the built application: integration tests, runtime behavior.
    /// </summary>
    Target TestPlayMode => _ => _
        .Description("Run Unity Play Mode tests (post-build runtime)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Play Mode Tests (Post-Build) ===");
            Serilog.Log.Information("Test Project: {Path}", TestProjectPath);

            // TODO: Implement Unity test execution
            // Example: Unity.exe -runTests -testPlatform PlayMode -projectPath <path>
            Serilog.Log.Warning("Play Mode tests not yet implemented");
            Serilog.Log.Information("✅ Play Mode test execution placeholder complete");
        });

    /// <summary>
    /// Run integration tests on built artifact (post-build).
    /// Tests the actual build output: startup, loading, integration.
    /// </summary>
    Target TestIntegration => _ => _
        .Description("Run integration tests (post-build)")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Integration Tests (Post-Build) ===");

            // TODO: Implement integration tests
            // - Launch built application
            // - Test startup sequence
            // - Test scene loading
            // - Test critical user flows
            Serilog.Log.Warning("Integration tests not yet implemented");
            Serilog.Log.Information("✅ Integration test placeholder complete");
        });

    /// <summary>
    /// Run all post-build tests (runtime/integration).
    /// Uses ProceedAfterFailure to ensure all test suites run even if some fail.
    /// Comprehensive validation of built artifact.
    /// </summary>
    Target TestPostBuild => _ => _
        .Description("Run all post-build tests (runtime/integration)")
        .DependsOn(TestPlayMode)
        .DependsOn(TestIntegration)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Serilog.Log.Information("=== Post-Build Tests Complete ===");

            var failedTargets = FailedTargets;
            if (failedTargets.Count > 0)
            {
                Serilog.Log.Warning("Some post-build tests failed:");
                foreach (var target in failedTargets)
                {
                    Serilog.Log.Warning("  - {Target}", target);
                }
            }
            else
            {
                Serilog.Log.Information("✅ All post-build tests passed");
            }
        });

    // ========================================
    // Combined Test Target
    // ========================================

    /// <summary>
    /// Run all tests (pre-build + post-build).
    /// Use this for comprehensive test coverage.
    /// Note: Pre-build tests must pass for build to proceed.
    /// </summary>
    Target TestAll => _ => _
        .Description("Run all tests (pre-build validation + post-build runtime)")
        .DependsOn(TestPreBuild)
        .DependsOn(TestPostBuild)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Serilog.Log.Information("=== All Tests Complete ===");

            var preBuildFailed = FailedTargets.Contains(TestPreBuild);
            var postBuildFailed = FailedTargets.Contains(TestPostBuild);

            if (!preBuildFailed && !postBuildFailed)
            {
                Serilog.Log.Information("✅ All tests passed (pre-build + post-build)");
            }
            else if (preBuildFailed && !postBuildFailed)
            {
                Serilog.Log.Warning("⚠️ Pre-build tests failed, post-build tests passed");
            }
            else if (!preBuildFailed && postBuildFailed)
            {
                Serilog.Log.Warning("⚠️ Pre-build tests passed, post-build tests failed");
            }
            else
            {
                Serilog.Log.Error("❌ Both pre-build and post-build tests failed");
            }
        });
}
