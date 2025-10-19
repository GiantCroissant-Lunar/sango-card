using Nuke.Common;

/// <summary>
/// Test build interface implementation for Build class.
///
/// ARCHITECTURE NOTE (R-CODE-090):
/// This partial class file contains ONLY the ITestBuild interface implementation.
/// The base NukeBuild inheritance is in Build.cs.
/// File naming: Build.TestBuild.cs (interface name without 'I' prefix)
/// This separation improves code organization and maintainability.
/// </summary>
partial class Build : ITestBuild
{
    // Interface implementation is provided by ITestBuild default interface members
    // Override default members here if custom implementation is needed

    /// <summary>
    /// Full build workflow with comprehensive testing.
    /// Flow: PrepareClient → TestPreBuild → BuildUnity → TestPostBuild → CleanupAfterBuild
    ///
    /// Pre-build tests (validation):
    /// - Edit Mode tests (unit tests)
    /// - Code quality checks
    /// - Fast feedback before expensive build
    ///
    /// Post-build tests (runtime):
    /// - Play Mode tests (integration)
    /// - Built artifact testing
    /// - Comprehensive validation
    ///
    /// Cleanup behavior:
    /// - Build success: Client restored regardless of test results
    /// - Build failure: Client state preserved for debugging
    /// </summary>
    Target BuildWithTests => _ => _
        .Description("Full build workflow with pre-build validation and post-build testing")
        .DependsOn(PrepareClient)
        .DependsOn(((ITestBuild)this).TestPreBuild)
        .DependsOn(((IUnityBuild)this).BuildUnity)
        .DependsOn(((ITestBuild)this).TestPostBuild)
        .DependsOn(CleanupAfterBuild)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Build with Tests Workflow Complete ===");

            var preBuildTestsSucceeded = SucceededTargets.Contains(((ITestBuild)this).TestPreBuild);
            var buildSucceeded = SucceededTargets.Contains(((IUnityBuild)this).BuildUnity);
            var postBuildTestsSucceeded = SucceededTargets.Contains(((ITestBuild)this).TestPostBuild);

            if (preBuildTestsSucceeded && buildSucceeded && postBuildTestsSucceeded)
            {
                Serilog.Log.Information("✅ All stages passed: Pre-build tests → Build → Post-build tests");
            }
            else if (!preBuildTestsSucceeded)
            {
                Serilog.Log.Error("❌ Pre-build tests failed - build may have been skipped");
            }
            else if (!buildSucceeded)
            {
                Serilog.Log.Error("❌ Build failed - post-build tests may not have run");
            }
            else if (!postBuildTestsSucceeded)
            {
                Serilog.Log.Warning("⚠️ Pre-build tests and build succeeded, but post-build tests failed");
                Serilog.Log.Warning("Check runtime test logs for details");
            }
        });

    /// <summary>
    /// Quick validation workflow (pre-build tests only).
    /// Fast feedback loop for development - runs validation without building.
    /// Use this for rapid iteration: edit code → validate → repeat.
    /// </summary>
    Target ValidateOnly => _ => _
        .Description("Run pre-build validation tests only (no build)")
        .DependsOn(PrepareClient)
        .DependsOn(((ITestBuild)this).TestPreBuild)
        .DependsOn(CleanupAfterBuild)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Validation Complete ===");

            var testsSucceeded = SucceededTargets.Contains(((ITestBuild)this).TestPreBuild);
            if (testsSucceeded)
            {
                Serilog.Log.Information("✅ Validation passed - ready to build");
            }
            else
            {
                Serilog.Log.Error("❌ Validation failed - fix issues before building");
            }
        });
}
