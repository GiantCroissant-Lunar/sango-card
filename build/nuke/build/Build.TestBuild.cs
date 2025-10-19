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
    /// Full build workflow with tests.
    /// Executes: PrepareCache → PrepareClient → BuildUnity → TestAll → CleanupAfterBuild
    /// Uses ProceedAfterFailure on TestAll to continue even if tests fail.
    /// CleanupAfterBuild uses AssuredAfterFailure to guarantee cleanup runs.
    /// On build success: Client restored regardless of test results
    /// On build failure: Client state preserved for debugging
    /// </summary>
    Target BuildWithTests => _ => _
        .Description("Full build workflow with tests (continues on test failure)")
        .DependsOn(PrepareClient)
        .DependsOn(((IUnityBuild)this).BuildUnity)
        .DependsOn(((ITestBuild)this).TestAll)
        .DependsOn(CleanupAfterBuild)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Build with Tests Workflow Complete ===");

            var buildSucceeded = SucceededTargets.Contains(((IUnityBuild)this).BuildUnity);
            var testsSucceeded = SucceededTargets.Contains(((ITestBuild)this).TestAll);

            if (buildSucceeded && testsSucceeded)
            {
                Serilog.Log.Information("✅ Build and all tests succeeded");
            }
            else if (buildSucceeded && !testsSucceeded)
            {
                Serilog.Log.Warning("⚠️ Build succeeded but some tests failed");
                Serilog.Log.Warning("Check test logs for details");
            }
            else
            {
                Serilog.Log.Error("❌ Build failed - tests may not have run");
            }
        });
}
