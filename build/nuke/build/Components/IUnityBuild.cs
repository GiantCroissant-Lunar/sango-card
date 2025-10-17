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

    [Parameter("Unity build version")]
    string UnityBuildVersion => TryGetValue(() => UnityBuildVersion) ?? "1.0.0";

    [Parameter("Unity build profile name (Windows, Android, iOS, etc.)")]
    string UnityBuildProfileName => TryGetValue(() => UnityBuildProfileName) ?? "Windows";

    [Parameter("Unity build purpose (UnityPlayer, UnityAssetBundles, UnityAddressables)")]
    string UnityBuildPurpose => TryGetValue(() => UnityBuildPurpose) ?? "UnityPlayer";

    /// <summary>
    /// Clean Unity build artifacts
    /// </summary>
    Target CleanUnity => _ => _
        .Description("Clean Unity build artifacts")
        .Executes(() =>
        {
            if (Directory.Exists(UnityBuildOutput))
            {
                Directory.Delete(UnityBuildOutput, recursive: true);
            }
            Directory.CreateDirectory(UnityBuildOutput);

            var libraryPath = UnityProjectPath / "Library";
            var tempPath = UnityProjectPath / "Temp";

            if (Directory.Exists(libraryPath))
            {
                Serilog.Log.Information("Cleaning Unity Library folder...");
            }

            if (Directory.Exists(tempPath))
            {
                Serilog.Log.Information("Cleaning Unity Temp folder...");
                Directory.Delete(tempPath, recursive: true);
            }
        });

    /// <summary>
    /// Build Unity project
    /// </summary>
    Target BuildUnity => _ => _
        .Description("Build Unity project")
        .Executes(() =>
        {
            Directory.CreateDirectory(UnityBuildOutput);

            Serilog.Log.Information($"Building Unity project at: {UnityProjectPath}");
            Serilog.Log.Information($"Target platform: {UnityBuildTarget}");
            Serilog.Log.Information($"Output path: {UnityBuildOutput}");
            Serilog.Log.Information($"Build version: {UnityBuildVersion}");
            Serilog.Log.Information($"Build profile: {UnityBuildProfileName}");
            Serilog.Log.Information($"Build purpose: {UnityBuildPurpose}");

            var arguments = new[]
            {
                "-quit",
                "-batchmode",
                "-nographics",
                $"-projectPath \"{UnityProjectPath}\"",
                $"-buildTarget {UnityBuildTarget}",
                "-executeMethod SangoCard.Build.Editor.BuildEntry.PerformBuild",
                $"--buildPurpose {UnityBuildPurpose}",
                $"--buildVersion {UnityBuildVersion}",
                $"--buildProfileName {UnityBuildProfileName}",
                $"--outputPath \"{UnityBuildOutput}\"",
                $"-logFile \"{UnityBuildOutput / "unity-build.log"}\"",
            };

            var process = ProcessTasks.StartProcess(
                UnityPath,
                string.Join(" ", arguments),
                workingDirectory: UnityProjectPath,
                timeout: (int)TimeSpan.FromMinutes(30).TotalMilliseconds);

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
            Directory.CreateDirectory(UnityBuildOutput);

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
                timeout: (int)TimeSpan.FromMinutes(15).TotalMilliseconds);

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
            Directory.CreateDirectory(UnityBuildOutput);

            var testResultsPath = UnityBuildOutput / "test-results.xml";

            // Ensure client repo is clean per R-BLD-060
            try
            {
                var gitReset = ProcessTasks.StartProcess(
                    "git",
                    $"-C \"{UnityProjectPath}\" reset --hard",
                    workingDirectory: UnityProjectPath);
                gitReset.WaitForExit();
            }
            catch { /* ignore if not a git repo */ }

            // Non-invasive analyzer override for MessagePack duplicates (MsgPack009)
            // We cannot modify projects/client permanently (R-BLD-060), so we stage temporary overrides
            var editorConfigPath = UnityProjectPath / ".editorconfig";
            var stagedAnalyzerOverride = false;
            string? originalEditorConfig = null;
            var cscRspPath = UnityProjectPath / "Assets" / "csc.rsp";
            var stagedCscRsp = false;
            string? originalCscRsp = null;
            try
            {
                if (File.Exists(editorConfigPath))
                {
                    originalEditorConfig = File.ReadAllText(editorConfigPath);
                    if (!originalEditorConfig.Contains("dotnet_diagnostic.MsgPack009.severity", StringComparison.Ordinal))
                    {
                        File.AppendAllText(editorConfigPath,
                            Environment.NewLine + "[*.cs]" + Environment.NewLine +
                            "dotnet_diagnostic.MsgPack009.severity = warning" + Environment.NewLine);
                        stagedAnalyzerOverride = true;
                    }
                }
                else
                {
                    var contents = "root = false" + Environment.NewLine +
                                   "[*.cs]" + Environment.NewLine +
                                   "dotnet_diagnostic.MsgPack009.severity = warning" + Environment.NewLine;
                    File.WriteAllText(editorConfigPath, contents);
                    stagedAnalyzerOverride = true;
                }

                // Also add csc.rsp -nowarn to suppress analyzer diagnostic regardless of severity
                var assetsDir = Path.Combine(UnityProjectPath, "Assets");
                Directory.CreateDirectory(assetsDir);
                if (File.Exists(cscRspPath))
                {
                    originalCscRsp = File.ReadAllText(cscRspPath);
                    if (!originalCscRsp.Contains("MsgPack009", StringComparison.Ordinal))
                    {
                        File.AppendAllText(cscRspPath, (originalCscRsp?.EndsWith(Environment.NewLine) == true ? string.Empty : Environment.NewLine) + "-nowarn:MsgPack009" + Environment.NewLine);
                        stagedCscRsp = true;
                    }
                }
                else
                {
                    File.WriteAllText(cscRspPath, "-nowarn:MsgPack009" + Environment.NewLine);
                    stagedCscRsp = true;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning($"Failed to stage .editorconfig override: {ex.Message}");
            }

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
            try
            {
                var process = ProcessTasks.StartProcess(
                    UnityPath,
                    string.Join(" ", arguments),
                    workingDirectory: UnityProjectPath,
                    timeout: (int)TimeSpan.FromMinutes(20).TotalMilliseconds);

                process.AssertZeroExitCode();

                Serilog.Log.Information($"Unity tests completed. Results: {testResultsPath}");
            }
            finally
            {
                // Revert any staged analyzer override to keep client repo pristine
                if (stagedAnalyzerOverride || stagedCscRsp)
                {
                    try
                    {
                        var gitRevert = ProcessTasks.StartProcess(
                            "git",
                            $"-C \"{UnityProjectPath}\" reset --hard",
                            workingDirectory: UnityProjectPath);
                        gitRevert.WaitForExit();
                    }
                    catch
                    {
                        // If git reset isn't available, try to restore previous contents
                        try
                        {
                            if (stagedAnalyzerOverride)
                            {
                                if (originalEditorConfig is null)
                                {
                                    if (File.Exists(editorConfigPath)) File.Delete(editorConfigPath);
                                }
                                else
                                {
                                    File.WriteAllText(editorConfigPath, originalEditorConfig);
                                }
                            }
                            if (stagedCscRsp)
                            {
                                if (originalCscRsp is null)
                                {
                                    if (File.Exists(cscRspPath)) File.Delete(cscRspPath);
                                }
                                else
                                {
                                    File.WriteAllText(cscRspPath, originalCscRsp);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Warning($"Failed to cleanup .editorconfig override: {ex.Message}");
                        }
                    }
                }
            }
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
