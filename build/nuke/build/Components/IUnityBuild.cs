using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;

/// <summary>
/// Reusable component for building Unity projects
/// </summary>
interface IUnityBuild : INukeBuild
{
    [Parameter("Unity executable path")]
    string UnityPath => TryGetValue(() => UnityPath) ?? GetDefaultUnityPath();

    [Parameter("Unity project path")]
    AbsolutePath UnityProjectPath => TryGetValue(() => UnityProjectPath) ?? RootDirectory;

    [Parameter("Unity build target platform (StandaloneWindows64, Android, iOS, etc.)")]
    string UnityBuildTarget => TryGetValue(() => UnityBuildTarget) ?? "StandaloneWindows64";

    [Parameter("Unity build output path")]
    AbsolutePath UnityBuildOutput => TryGetValue(() => UnityBuildOutput) ?? RootDirectory / "output";

    /// <summary>
    /// Clean Unity build artifacts
    /// </summary>
    Target CleanUnity => _ => _
        .Description("Clean Unity build artifacts")
        .Executes(() =>
        {
            EnsureCleanDirectory(UnityBuildOutput);
            
            var libraryPath = UnityProjectPath / "Library";
            var tempPath = UnityProjectPath / "Temp";
            
            if (Directory.Exists(libraryPath))
            {
                Serilog.Log.Information("Cleaning Unity Library folder...");
            }
            
            if (Directory.Exists(tempPath))
            {
                Serilog.Log.Information("Cleaning Unity Temp folder...");
                DeleteDirectory(tempPath);
            }
        });

    /// <summary>
    /// Build Unity project
    /// </summary>
    Target BuildUnity => _ => _
        .Description("Build Unity project")
        .Executes(() =>
        {
            EnsureExistingDirectory(UnityBuildOutput);
            
            Serilog.Log.Information($"Building Unity project at: {UnityProjectPath}");
            Serilog.Log.Information($"Target platform: {UnityBuildTarget}");
            Serilog.Log.Information($"Output path: {UnityBuildOutput}");
            
            var arguments = new[]
            {
                "-quit",
                "-batchmode",
                "-nographics",
                $"-projectPath \"{UnityProjectPath}\"",
                $"-buildTarget {UnityBuildTarget}",
                "-executeMethod BuildScript.Build",
                $"-logFile \"{UnityBuildOutput / "unity-build.log"}\"",
            };

            var process = ProcessTasks.StartProcess(
                UnityPath,
                string.Join(" ", arguments),
                workingDirectory: UnityProjectPath,
                timeout: TimeSpan.FromMinutes(30));

            process.AssertZeroExitCode();
            
            Serilog.Log.Information("Unity build completed successfully");
        });

    /// <summary>
    /// Export Unity package
    /// </summary>
    Target ExportUnityPackage => _ => _
        .Description("Export Unity package")
        .Executes(() =>
        {
            EnsureExistingDirectory(UnityBuildOutput);
            
            var packagePath = UnityBuildOutput / "package.unitypackage";
            
            var arguments = new[]
            {
                "-quit",
                "-batchmode",
                "-nographics",
                $"-projectPath \"{UnityProjectPath}\"",
                "-exportPackage Assets",
                $"\"{packagePath}\"",
                $"-logFile \"{UnityBuildOutput / "unity-export.log"}\"",
            };

            var process = ProcessTasks.StartProcess(
                UnityPath,
                string.Join(" ", arguments),
                workingDirectory: UnityProjectPath,
                timeout: TimeSpan.FromMinutes(15));

            process.AssertZeroExitCode();
            
            Serilog.Log.Information($"Unity package exported to: {packagePath}");
        });

    /// <summary>
    /// Run Unity tests
    /// </summary>
    Target TestUnity => _ => _
        .Description("Run Unity tests")
        .Executes(() =>
        {
            EnsureExistingDirectory(UnityBuildOutput);
            
            var testResultsPath = UnityBuildOutput / "test-results.xml";
            
            var arguments = new[]
            {
                "-quit",
                "-batchmode",
                "-nographics",
                $"-projectPath \"{UnityProjectPath}\"",
                "-runTests",
                "-testPlatform EditMode",
                $"-testResults \"{testResultsPath}\"",
                $"-logFile \"{UnityBuildOutput / "unity-test.log"}\"",
            };

            var process = ProcessTasks.StartProcess(
                UnityPath,
                string.Join(" ", arguments),
                workingDirectory: UnityProjectPath,
                timeout: TimeSpan.FromMinutes(20));

            process.AssertZeroExitCode();
            
            Serilog.Log.Information($"Unity tests completed. Results: {testResultsPath}");
        });

    /// <summary>
    /// Get default Unity installation path based on platform
    /// </summary>
    private string GetDefaultUnityPath()
    {
        if (EnvironmentInfo.IsWin)
        {
            var defaultPath = @"C:\Program Files\Unity\Hub\Editor\2022.3.0f1\Editor\Unity.exe";
            if (File.Exists(defaultPath))
                return defaultPath;
            
            // Check for Unity Hub installations
            var hubPath = @"C:\Program Files\Unity\Hub\Editor";
            if (Directory.Exists(hubPath))
            {
                var versions = Directory.GetDirectories(hubPath);
                if (versions.Length > 0)
                {
                    var latestVersion = versions.OrderByDescending(v => v).First();
                    return Path.Combine(latestVersion, "Editor", "Unity.exe");
                }
            }
        }
        else if (EnvironmentInfo.IsOsx)
        {
            return "/Applications/Unity/Hub/Editor/2022.3.0f1/Unity.app/Contents/MacOS/Unity";
        }
        else if (EnvironmentInfo.IsUnix)
        {
            return "/opt/unity/Editor/Unity";
        }

        throw new Exception("Unity executable not found. Please specify using --unity-path parameter.");
    }
}
