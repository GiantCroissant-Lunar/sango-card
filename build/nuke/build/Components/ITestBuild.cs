using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;

/// <summary>
/// Test build component interface for running Unity tests.
/// Designed to work with ProceedAfterFailure to allow build continuation even if tests fail.
/// </summary>
interface ITestBuild : INukeBuild
{
    /// <summary>
    /// Path to Unity project for testing (typically same as build project).
    /// </summary>
    AbsolutePath TestProjectPath => ((IReportBuild)this).RepoRoot / "projects" / "client";

    /// <summary>
    /// Run Unity Edit Mode tests.
    /// </summary>
    Target TestEditMode => _ => _
        .Description("Run Unity Edit Mode tests")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Edit Mode Tests ===");
            Serilog.Log.Information("Test Project: {Path}", TestProjectPath);

            // TODO: Implement Unity test execution
            // Example: Unity.exe -runTests -testPlatform EditMode -projectPath <path>
            Serilog.Log.Warning("Edit Mode tests not yet implemented");
            Serilog.Log.Information("✅ Edit Mode test execution placeholder complete");
        });

    /// <summary>
    /// Run Unity Play Mode tests.
    /// </summary>
    Target TestPlayMode => _ => _
        .Description("Run Unity Play Mode tests")
        .Executes(() =>
        {
            Serilog.Log.Information("=== Running Play Mode Tests ===");
            Serilog.Log.Information("Test Project: {Path}", TestProjectPath);

            // TODO: Implement Unity test execution
            // Example: Unity.exe -runTests -testPlatform PlayMode -projectPath <path>
            Serilog.Log.Warning("Play Mode tests not yet implemented");
            Serilog.Log.Information("✅ Play Mode test execution placeholder complete");
        });

    /// <summary>
    /// Run all Unity tests (Edit Mode + Play Mode).
    /// Uses ProceedAfterFailure to ensure all test suites run even if some fail.
    /// </summary>
    Target TestAll => _ => _
        .Description("Run all Unity tests (continues on failure)")
        .DependsOn(TestEditMode)
        .DependsOn(TestPlayMode)
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Serilog.Log.Information("=== All Unity Tests Complete ===");

            var failedTargets = FailedTargets;
            if (failedTargets.Count > 0)
            {
                Serilog.Log.Warning("Some test suites failed:");
                foreach (var target in failedTargets)
                {
                    Serilog.Log.Warning("  - {Target}", target);
                }
            }
            else
            {
                Serilog.Log.Information("✅ All test suites passed");
            }
        });
}
