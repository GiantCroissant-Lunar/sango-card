using System;
using System.IO;
using System.Collections.Generic;
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

    [Parameter("Use isolated Unity project for tests (avoids client repo)")]
    string UseIsolatedTestsOption => TryGetValue(() => UseIsolatedTestsOption) ?? "true";

    [Parameter("Path of isolated Unity project for tests")]
    AbsolutePath IsolatedUnityProjectPath => TryGetValue(() => IsolatedUnityProjectPath) ?? RootDirectory / "output" / "unity-isolated";

    // Build Tool (dotnet~/tool) integration
    [Parameter("Run Build Prep Tool before tests (prepare run)")]
    string UseBuildToolForTestsOption => TryGetValue(() => UseBuildToolForTestsOption) ?? "false";

    [Parameter("Build Tool project path (csproj)")]
    AbsolutePath BuildToolProjectPath => TryGetValue(() => BuildToolProjectPath) ??
        RootDirectory / "packages" / "scoped-6571" / "com.contractwork.sangocard.build" / "dotnet~" / "tool" / "SangoCard.Build.Tool" / "SangoCard.Build.Tool.csproj";

    [Parameter("Build Tool config file path")]
    string? BuildToolConfigPath => TryGetValue(() => BuildToolConfigPath);

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
            var useIsolated = string.Equals(UseIsolatedTestsOption, "true", StringComparison.OrdinalIgnoreCase) || UseIsolatedTestsOption == "1";
            var testProjectPath = useIsolated
                ? PrepareIsolatedUnityProjectForTests()
                : UnityProjectPath;

            bool stagedAnalyzerOverride = false;
            bool stagedCscRsp = false;
            string? originalEditorConfig = null;
            string? originalCscRsp = null;
            var editorConfigPath = testProjectPath / ".editorconfig";
            var cscRspPath = testProjectPath / "Assets" / "csc.rsp";

            if (!useIsolated)
            {
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

                // Stage temporary overrides only when using client project
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

                    var assetsDir = Path.Combine(testProjectPath, "Assets");
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
                    Serilog.Log.Warning($"Failed to stage overrides: {ex.Message}");
                }
            }

            var projectPathArg = useIsolated
                ? Path.GetRelativePath(RootDirectory, testProjectPath)
                : testProjectPath.ToString();
            var arguments = new[]
            {
                "-quit",
                "-batchmode",
                "-nographics",
                $"-projectPath \"{projectPathArg}\"",
                "-runTests",
                "-testPlatform EditMode",
                $"-testResults \"{testResultsPath}\"",
                $"-logFile \"{UnityBuildOutput / "unity-test.log"}\"",
            };
            try
            {
                // Optionally run the build preparation tool against the selected project
                var runBuildTool = string.Equals(UseBuildToolForTestsOption, "true", StringComparison.OrdinalIgnoreCase) || UseBuildToolForTestsOption == "1";
                if (runBuildTool)
                {
                    RunBuildPreparationTool(testProjectPath);
                }

                var process = ProcessTasks.StartProcess(
                    UnityPath,
                    string.Join(" ", arguments),
                    workingDirectory: (useIsolated ? RootDirectory : testProjectPath),
                    timeout: (int)TimeSpan.FromMinutes(20).TotalMilliseconds);

                process.AssertZeroExitCode();

                Serilog.Log.Information($"Unity tests completed. Results: {testResultsPath}");
            }
            finally
            {
                // Revert any staged analyzer override to keep client repo pristine
                if (!useIsolated && (stagedAnalyzerOverride || stagedCscRsp))
                {
                    try
                    {
                        var gitRevert = ProcessTasks.StartProcess(
                            "git",
                            $"-C \"{testProjectPath}\" reset --hard",
                            workingDirectory: testProjectPath);
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

    private void RunBuildPreparationTool(AbsolutePath targetProjectPath)
    {
        if (!File.Exists(BuildToolProjectPath))
        {
            Serilog.Log.Warning($"Build Tool project not found at '{BuildToolProjectPath}'. Skipping build preparation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(BuildToolConfigPath))
        {
            Serilog.Log.Warning("Build Tool config path is not set. Skipping build preparation (requires --config).");
            return;
        }

        // Resolve config path relative to git root (RootDirectory)
        var configArg = BuildToolConfigPath!;
        var configAbs = Path.IsPathRooted(configArg) ? configArg : (RootDirectory / configArg).ToString();
        if (!File.Exists(configAbs))
        {
            Serilog.Log.Warning($"Build Tool config file not found at '{configAbs}'. Skipping build preparation.");
            return;
        }

        var dotnet = Environment.GetEnvironmentVariable("DOTNET_EXE");
        if (string.IsNullOrWhiteSpace(dotnet))
        {
            dotnet = ToolPathResolver.GetPathExecutable("dotnet") ?? "dotnet";
        }

        var args = new List<string>
        {
            "run",
            "--project",
            $"\"{BuildToolProjectPath}\"",
            "--",
            "prepare",
            "run",
            "--config",
            $"\"{configAbs}\""
        };

        Serilog.Log.Information("Running Build Preparation Tool: {Args}", string.Join(" ", args));
        var proc = ProcessTasks.StartProcess(dotnet, string.Join(" ", args), workingDirectory: RootDirectory);
        proc.AssertZeroExitCode();
    }

    /// <summary>
    /// Prepares an isolated Unity project that embeds our local packages.
    /// This avoids loading the client repository and its scripts (R-BLD-060).
    /// </summary>
    private AbsolutePath PrepareIsolatedUnityProjectForTests()
    {
        var isoPath = IsolatedUnityProjectPath;
        if (Directory.Exists(isoPath))
        {
            var unique = $"unity-isolated-{DateTime.Now:yyyyMMddHHmmssfff}";
            isoPath = RootDirectory / "output" / unique;
        }
        Directory.CreateDirectory(isoPath);

        // Create a fresh Unity project at the path so -projectPath is valid
        var createArgs = new[]
        {
            "-quit",
            "-batchmode",
            "-nographics",
            $"-createProject \"{isoPath}\"",
            $"-logFile \"{UnityBuildOutput / "unity-create.log"}\"",
        };
        var createProc = ProcessTasks.StartProcess(
            UnityPath,
            string.Join(" ", createArgs),
            workingDirectory: RootDirectory,
            timeout: (int)TimeSpan.FromMinutes(10).TotalMilliseconds);
        createProc.AssertZeroExitCode();

        var packagesDir = isoPath / "Packages";
        Directory.CreateDirectory(packagesDir);

        // Minimal manifest with test framework to enable EditMode tests
        var manifestPath = packagesDir / "manifest.json";
        var manifest = "{\n" +
            "  \"dependencies\": {\n" +
            "    \"com.unity.test-framework\": \"1.4.5\"\n" +
            "  }\n" +
            "}\n";
        File.WriteAllText(manifestPath, manifest);

        // Ensure C# language level supports file-scoped namespaces (C# 10+)
        var assetsDirIso = isoPath / "Assets";
        Directory.CreateDirectory(assetsDirIso);
        var cscRspIso = assetsDirIso / "csc.rsp";
        File.WriteAllText(cscRspIso, "-langversion:preview" + Environment.NewLine);

        // Copy our local packages into the isolated project's Packages folder
        var rootPackages = RootDirectory / "packages";
        if (Directory.Exists(rootPackages))
        {
            var candidates = Directory.GetDirectories(rootPackages, "com.contractwork.sangocard.*", SearchOption.AllDirectories);
            foreach (var src in candidates)
            {
                var name = Path.GetFileName(src);
                var dest = packagesDir / name;
                CopyDirectoryRecursively(src, dest);
                Serilog.Log.Information($"Embedded package: {name}");
            }
        }
        else
        {
            Serilog.Log.Warning("No local packages directory found at 'packages'. Isolated project may be empty.");
        }

        return isoPath;
    }

    private static void CopyDirectoryRecursively(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var target = Path.Combine(destDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
