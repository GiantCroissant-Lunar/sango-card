using System;
using System.Linq;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

/// <summary>
/// Reusable component for building SangoCard Reporting libraries.
/// Handles Reporting.Abstractions and Reporting.Core projects.
/// </summary>
interface IReportBuild : INukeBuild
{
    // Versioning via GitVersion
    [GitVersion(NoFetch = true)] GitVersion GitVersion => TryGetValue(() => GitVersion);

    // Parameters
    [Parameter("Report configuration (Debug/Release)")]
    Configuration ReportConfiguration => TryGetValue(() => ReportConfiguration) ?? (IsLocalBuild ? Configuration.Debug : Configuration.Release);

    // Resolve repository root robustly (RootDirectory may be the build folder when executed from there)
    AbsolutePath RepoRoot =>
        Directory.Exists((RootDirectory / ".git").ToString())
            ? RootDirectory
            : (AbsolutePath)System.IO.Path.GetFullPath(((BuildProjectDirectory / ".." / ".." / "..").ToString()));

    [Parameter("Report solution path (default: dotnet/report/SangoCard.sln)")]
    AbsolutePath ReportSolutionPath => TryGetValue(() => ReportSolutionPath) ?? RepoRoot / "dotnet" / "report" / "SangoCard.sln";

    [Parameter("Report artifacts root directory (default: build/_artifacts)")]
    AbsolutePath ReportArtifactsRoot => TryGetValue(() => ReportArtifactsRoot) ?? RepoRoot / "build" / "_artifacts";

    // Outputs aligned to user's spec
    // NuGet packages: build/_artifacts/<version>/nuget-packages
    [Parameter("Output directory for NuGet packages (default: <ArtifactsRoot>/<VersionFolder>/nuget-packages)")]
    AbsolutePath ReportPackagesOutput => TryGetValue(() => ReportPackagesOutput) ?? ReportArtifactsRoot / GetReportVersionFolder() / "nuget-packages";

    // Report documents: build/_artifacts/<version>/build/report
    [Parameter("Output directory for report documents (default: <ArtifactsRoot>/<VersionFolder>/build/report)")]
    AbsolutePath ReportDocumentsOutput => TryGetValue(() => ReportDocumentsOutput) ?? ReportArtifactsRoot / GetReportVersionFolder() / "build" / "report";

    [Parameter("Version suffix for report packages (e.g., 1.2.3 or ci-1234)")]
    string ReportVersionSuffix => TryGetValue(() => ReportVersionSuffix);

    [Parameter("Report target framework override (optional)")]
    string ReportFramework => TryGetValue(() => ReportFramework);

    [Parameter("Preparation config files to apply (comma-separated, e.g., prep.json,production.json)")]
    string PrepConfigs => TryGetValue(() => PrepConfigs) ?? "preparation.json";

    // Access PreparationConfig if available (from Build.Preparation.cs)
    AbsolutePath ActualPreparationConfig =>
        this is Build build && build.PreparationConfig != null
            ? build.PreparationConfig
            : RepoRoot / "build" / "preparation" / "configs" / "preparation.json";

    // Project paths
    AbsolutePath ReportDirectory =>
        Directory.Exists((RepoRoot / "dotnet" / "report").ToString())
            ? RepoRoot / "dotnet" / "report"
            : (AbsolutePath)System.IO.Path.GetFullPath(((BuildProjectDirectory / ".." / ".." / ".." / "dotnet" / "report").ToString()));
    AbsolutePath ReportAbstractionsProject => ReportDirectory / "Reporting.Abstractions" / "Reporting.Abstractions.csproj";
    AbsolutePath ReportCoreProject => ReportDirectory / "Reporting.Core" / "Reporting.Core.csproj";
    AbsolutePath ReportTestsDirectory => ReportDirectory / "tests";

    // Optional publish output root (not NuGet): build/_artifacts/<version>/publish
    AbsolutePath ReportPublishOutputRoot => ReportArtifactsRoot / GetReportVersionFolder() / "publish";

    // Helper to get version folder name
    private string GetReportVersionFolder()
    {
        var name = ReportVersionSuffix;
        if (string.IsNullOrWhiteSpace(name) && GitVersion != null)
            name = GitVersion.NuGetVersionV2 ?? GitVersion.SemVer ?? GitVersion.InformationalVersion;
        if (string.IsNullOrWhiteSpace(name))
            name = IsLocalBuild ? "local" : "ci";
        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name;
    }

    // Targets
    Target CleanReport => _ => _
        .Description("Clean report build artifacts")
        .Executes(() =>
        {
            Serilog.Log.Information("Cleaning report artifacts...");
            Directory.CreateDirectory(ReportArtifactsRoot);
            Directory.CreateDirectory(ReportPackagesOutput);
            Directory.CreateDirectory(ReportDocumentsOutput);

            // Clean bin/obj directories
            ReportDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d =>
            {
                if (Directory.Exists(d))
                {
                    Serilog.Log.Debug("Deleting: {Directory}", d);
                    Directory.Delete(d, true);
                }
            });

            Serilog.Log.Information("Report artifacts cleaned.");
        });

    Target RestoreReport => _ => _
        .Description("Restore NuGet packages for report solution")
        .Executes(() =>
        {
            Serilog.Log.Information("Restoring report solution: {Solution}", ReportSolutionPath);

            if (!File.Exists(ReportSolutionPath))
            {
                Serilog.Log.Warning("Report solution not found: {Solution}", ReportSolutionPath);
                return;
            }

            DotNetRestore(s => s
                .SetProjectFile(ReportSolutionPath)
            );

            Serilog.Log.Information("Report solution restored.");
        });

    Target BuildReport => _ => _
        .Description("Build report libraries (Abstractions + Core)")
        .DependsOn(RestoreReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Building report solution: {Solution}", ReportSolutionPath);

            if (!File.Exists(ReportSolutionPath))
            {
                Serilog.Log.Error("Report solution not found: {Solution}", ReportSolutionPath);
                throw new FileNotFoundException($"Report solution not found: {ReportSolutionPath}");
            }

            DotNetBuild(s =>
            {
                var cfg = s
                    .SetProjectFile(ReportSolutionPath)
                    .SetConfiguration(ReportConfiguration)
                    .EnableNoRestore();

                if (!string.IsNullOrEmpty(ReportFramework))
                    cfg = cfg.SetFramework(ReportFramework);

                if (!string.IsNullOrEmpty(ReportVersionSuffix))
                    cfg = cfg.SetVersionSuffix(ReportVersionSuffix);
                else if (GitVersion != null)
                    cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);

                return cfg;
            });

            Serilog.Log.Information("Report solution built successfully.");
        });

    Target TestReport => _ => _
        .Description("Run unit tests for report libraries")
        .DependsOn(BuildReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Running report tests...");

            if (!Directory.Exists(ReportTestsDirectory))
            {
                Serilog.Log.Warning("Report tests directory not found: {Directory}", ReportTestsDirectory);
                return;
            }

            DotNetTest(s => s
                .SetProjectFile(ReportSolutionPath)
                .SetConfiguration(ReportConfiguration)
                .EnableNoBuild()
                .EnableNoRestore()
            );

            Serilog.Log.Information("Report tests completed.");
        });

    Target PackReport => _ => _
        .Description("Pack report libraries as NuGet packages")
        .DependsOn(BuildReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Packing report libraries to: {Output}", ReportPackagesOutput);

            Directory.CreateDirectory(ReportPackagesOutput);

            // Pack Reporting.Abstractions
            if (File.Exists(ReportAbstractionsProject))
            {
                Serilog.Log.Information("Packing Reporting.Abstractions...");
                DotNetPack(s =>
                {
                    var cfg = s
                        .SetProject(ReportAbstractionsProject)
                        .SetConfiguration(ReportConfiguration)
                        .SetOutputDirectory(ReportPackagesOutput)
                        .EnableNoBuild()
                        .EnableNoRestore();

                    if (!string.IsNullOrEmpty(ReportVersionSuffix))
                        cfg = cfg.SetVersionSuffix(ReportVersionSuffix);
                    else if (GitVersion != null)
                        cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);

                    return cfg;
                });
            }

            // Pack Reporting.Core
            if (File.Exists(ReportCoreProject))
            {
                Serilog.Log.Information("Packing Reporting.Core...");
                DotNetPack(s =>
                {
                    var cfg = s
                        .SetProject(ReportCoreProject)
                        .SetConfiguration(ReportConfiguration)
                        .SetOutputDirectory(ReportPackagesOutput)
                        .EnableNoBuild()
                        .EnableNoRestore();

                    if (!string.IsNullOrEmpty(ReportVersionSuffix))
                        cfg = cfg.SetVersionSuffix(ReportVersionSuffix);
                    else if (GitVersion != null)
                        cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);

                    return cfg;
                });
            }

            Serilog.Log.Information("Report libraries packed successfully.");
            Serilog.Log.Information("NuGet packages: {Output}", ReportPackagesOutput);
        });

    Target PublishReportAbstractions => _ => _
        .Description("Publish Reporting.Abstractions for distribution")
        .DependsOn(BuildReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Publishing Reporting.Abstractions...");

            if (!File.Exists(ReportAbstractionsProject))
            {
                Serilog.Log.Error("Reporting.Abstractions project not found: {Project}", ReportAbstractionsProject);
                throw new FileNotFoundException($"Project not found: {ReportAbstractionsProject}");
            }

            var outputDir = ReportPublishOutputRoot / "Reporting.Abstractions";
            Directory.CreateDirectory(outputDir);

            DotNetPublish(s =>
            {
                var cfg = s
                    .SetProject(ReportAbstractionsProject)
                    .SetConfiguration(ReportConfiguration)
                    .SetOutput(outputDir)
                    .EnableNoBuild()
                    .EnableNoRestore();

                if (!string.IsNullOrEmpty(ReportFramework))
                    cfg = cfg.SetFramework(ReportFramework);

                if (!string.IsNullOrEmpty(ReportVersionSuffix))
                    cfg = cfg.SetVersionSuffix(ReportVersionSuffix);
                else if (GitVersion != null)
                    cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);

                return cfg;
            });

            Serilog.Log.Information("Reporting.Abstractions published to: {Output}", outputDir);
        });

    Target PublishReportCore => _ => _
        .Description("Publish Reporting.Core for distribution")
        .DependsOn(BuildReport)
        .Executes(() =>
        {
            Serilog.Log.Information("Publishing Reporting.Core...");

            if (!File.Exists(ReportCoreProject))
            {
                Serilog.Log.Error("Reporting.Core project not found: {Project}", ReportCoreProject);
                throw new FileNotFoundException($"Project not found: {ReportCoreProject}");
            }

            var outputDir = ReportPublishOutputRoot / "Reporting.Core";
            Directory.CreateDirectory(outputDir);

            DotNetPublish(s =>
            {
                var cfg = s
                    .SetProject(ReportCoreProject)
                    .SetConfiguration(ReportConfiguration)
                    .SetOutput(outputDir)
                    .EnableNoBuild()
                    .EnableNoRestore();

                if (!string.IsNullOrEmpty(ReportFramework))
                    cfg = cfg.SetFramework(ReportFramework);

                if (!string.IsNullOrEmpty(ReportVersionSuffix))
                    cfg = cfg.SetVersionSuffix(ReportVersionSuffix);
                else if (GitVersion != null)
                    cfg = cfg.SetVersion(GitVersion.NuGetVersionV2);

                return cfg;
            });

            Serilog.Log.Information("Reporting.Core published to: {Output}", outputDir);
        });

    Target PublishReport => _ => _
        .Description("Publish all report libraries")
        .DependsOn(PublishReportAbstractions, PublishReportCore)
        .Executes(() =>
        {
            Serilog.Log.Information("All report libraries published successfully.");
        });

    Target ReportFull => _ => _
        .Description("Full report build pipeline: clean, build, test, pack")
        .DependsOn(CleanReport, BuildReport, TestReport, PackReport, GenerateReport)
        .Executes(() =>
        {
            Serilog.Log.Information("=== Report Build Pipeline Complete ===");
            Serilog.Log.Information("Configuration: {Configuration}", ReportConfiguration);
            Serilog.Log.Information("Packages: {Output}", ReportPackagesOutput);
            Serilog.Log.Information("Reports:  {Reports}", ReportDocumentsOutput);

            // List generated packages
            if (Directory.Exists(ReportPackagesOutput))
            {
                var packages = Directory.GetFiles(ReportPackagesOutput, "*.nupkg");
                if (packages.Length > 0)
                {
                    Serilog.Log.Information("Generated packages:");
                    foreach (var pkg in packages)
                    {
                        var fileInfo = new FileInfo(pkg);
                        Serilog.Log.Information("  - {Package} ({Size:N0} bytes)", Path.GetFileName(pkg), fileInfo.Length);
                    }
                }
            }
        });

    AbsolutePath ReportingCliProject => ReportDirectory / "Reporting.Cli" / "Reporting.Cli.csproj";

    Target GenerateReport => _ => _
        .Description("Generate report documents (JSON/Markdown) into build/_artifacts/<version>/build/report")
        .DependsOn(BuildReport)
        .Executes(() =>
        {
            Directory.CreateDirectory(ReportDocumentsOutput);

            if (!File.Exists(ReportingCliProject))
            {
                Serilog.Log.Warning("Reporting.Cli project not found at {Path}. Skipping GenerateReport.", ReportingCliProject);
                return;
            }

            var versionArg = !string.IsNullOrWhiteSpace(ReportVersionSuffix)
                ? ReportVersionSuffix
                : (GitVersion?.NuGetVersionV2 ?? GitVersion?.SemVer ?? GitVersion?.InformationalVersion ?? GetReportVersionFolder());
            var args = $"run --project \"{ReportingCliProject}\" --configuration {ReportConfiguration} -- --output \"{ReportDocumentsOutput}\"";
            if (!string.IsNullOrWhiteSpace(versionArg)) args += $" --artifacts-version \"{versionArg}\"";
            args += " --formats json md";
            DotNet(args);

            Serilog.Log.Information("Report documents generated at: {Path}", ReportDocumentsOutput);
        });

    Target GenerateReportDryRun => _ => _
        .Description("Generate DRY-RUN report documents (JSON/Markdown) into build/_artifacts/<version>/build/report")
        .DependsOn(PreviewCodePatches)
        .Executes(() =>
        {
            Directory.CreateDirectory(ReportDocumentsOutput);

            var versionArg = !string.IsNullOrWhiteSpace(ReportVersionSuffix)
                ? ReportVersionSuffix
                : (GitVersion?.NuGetVersionV2 ?? GitVersion?.SemVer ?? GitVersion?.InformationalVersion ?? GetReportVersionFolder());

            var baseName = $"build-info-dryrun";
            var jsonPath = (ReportDocumentsOutput / (baseName + ".json")).ToString();
            var mdPath = (ReportDocumentsOutput / (baseName + ".md")).ToString();

            // Collect configuration details
            var configuration = new System.Collections.Generic.Dictionary<string, string>
            {
                ["Configuration"] = ReportConfiguration.ToString(),
                ["RepoRoot"] = RepoRoot.ToString(),
                ["SolutionPath"] = ReportSolutionPath.ToString(),
                ["ArtifactsRoot"] = ReportArtifactsRoot.ToString(),
                ["PackagesOutput"] = ReportPackagesOutput.ToString(),
                ["DocumentsOutput"] = ReportDocumentsOutput.ToString(),
                ["Framework"] = string.IsNullOrEmpty(ReportFramework) ? "Default" : ReportFramework,
                ["GitVersion"] = GitVersion?.NuGetVersionV2 ?? "Not Available",
                ["IsLocalBuild"] = IsLocalBuild.ToString(),
                ["BuildProjectDirectory"] = BuildProjectDirectory.ToString()
            };

            // Track which preparation configs are being applied
            var appliedConfigs = new System.Collections.Generic.List<(string name, string path, bool exists, string description)>();

            // Collect operation details from build system
            var operations = new System.Collections.Generic.List<(string name, string source, string target, bool success)>
            {
                ("Report Generation", ReportDirectory.ToString(), ReportDocumentsOutput.ToString(), true),
                ("NuGet Packages", ReportDirectory.ToString(), ReportPackagesOutput.ToString(), Directory.Exists(ReportPackagesOutput)),
                ("Solution Build", ReportSolutionPath.ToString(), "N/A", File.Exists(ReportSolutionPath))
            };

            // Use the actual PreparationConfig parameter if available
            var prepConfigPath = ActualPreparationConfig;
            var configName = System.IO.Path.GetFileName(prepConfigPath.ToString());

            if (true) // Single config mode
            {
                if (File.Exists(prepConfigPath))
                {
                    try
                    {
                        var prepConfigJson = File.ReadAllText(prepConfigPath);
                        var prepConfig = System.Text.Json.JsonDocument.Parse(prepConfigJson);
                        var root = prepConfig.RootElement;

                        // Record this config as being applied
                        var configDesc = root.TryGetProperty("description", out var desc) ? desc.GetString() : "No description";
                        appliedConfigs.Add((configName, prepConfigPath.ToString(), true, configDesc ?? "No description"));

                    // Check packages
                    if (root.TryGetProperty("packages", out var packages))
                    {
                        foreach (var pkg in packages.EnumerateArray())
                        {
                            var name = pkg.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                            var source = pkg.TryGetProperty("source", out var s) ? s.GetString() : "";
                            var target = pkg.TryGetProperty("target", out var t) ? t.GetString() : "";
                            var sourcePath = RepoRoot / source;
                            // Check if path exists as file, directory, or symlink
                            var success = File.Exists(sourcePath) || Directory.Exists(sourcePath);
                            operations.Add(($"Package: {name}", sourcePath.ToString(), target, success));
                        }
                    }

                    // Check assemblies
                    if (root.TryGetProperty("assemblies", out var assemblies))
                    {
                        foreach (var asm in assemblies.EnumerateArray())
                        {
                            var name = asm.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                            var source = asm.TryGetProperty("source", out var s) ? s.GetString() : "";
                            var target = asm.TryGetProperty("target", out var t) ? t.GetString() : "";
                            var sourcePath = RepoRoot / source;
                            // Check if path exists as file, directory, or symlink
                            var success = File.Exists(sourcePath) || Directory.Exists(sourcePath);
                            operations.Add(($"Assembly: {name}", sourcePath.ToString(), target, success));
                        }
                    }

                    // Check asset manipulations
                    if (root.TryGetProperty("assetManipulations", out var assetOps))
                    {
                        foreach (var op in assetOps.EnumerateArray())
                        {
                            var operation = op.TryGetProperty("operation", out var o) ? o.GetString() : "Unknown";
                            var source = op.TryGetProperty("source", out var s) ? s.GetString() : "";
                            var target = op.TryGetProperty("target", out var t) ? t.GetString() : "";

                            if (operation == "Delete")
                            {
                                var targetPath = RepoRoot / target;
                                var success = File.Exists(targetPath) || Directory.Exists(targetPath);
                                operations.Add(($"Delete: {System.IO.Path.GetFileName(target)}", "N/A", targetPath.ToString(), success));
                            }
                            else
                            {
                                var sourcePath = RepoRoot / source;
                                var targetPath = RepoRoot / target;
                                var success = File.Exists(sourcePath) || Directory.Exists(sourcePath);
                                operations.Add(($"{operation}: {System.IO.Path.GetFileName(source)}", sourcePath.ToString(), targetPath.ToString(), success));
                            }
                        }
                    }

                    // Check code patches
                    if (root.TryGetProperty("codePatches", out var patches))
                    {
                        foreach (var patch in patches.EnumerateArray())
                        {
                            var file = patch.TryGetProperty("file", out var f) ? f.GetString() : "";
                            var filePath = RepoRoot / file;
                            // Check if path exists as file or symlink
                            var success = File.Exists(filePath) || Directory.Exists(filePath);

                            // Count patches for this file
                            var patchCount = 0;
                            if (patch.TryGetProperty("patches", out var patchList))
                            {
                                foreach (var _ in patchList.EnumerateArray())
                                {
                                    patchCount++;
                                }
                            }

                            operations.Add(($"Code Patch: {System.IO.Path.GetFileName(file)} ({patchCount} patch(es))", filePath.ToString(), "In-place", success));
                        }
                    }
                    }
                    catch (System.Exception ex)
                    {
                        Serilog.Log.Warning("Failed to parse {ConfigName}: {Error}", configName, ex.Message);
                    }
                }
                else
                {
                    // Config file not found - record it as missing
                    appliedConfigs.Add((configName, prepConfigPath.ToString(), false, "Config file not found"));
                    Serilog.Log.Warning("Preparation config not found: {ConfigPath}", prepConfigPath);
                }
            } // End single config mode

            // Build JSON with configuration, applied configs, and operations
            var jsonBuilder = new System.Text.StringBuilder();
            jsonBuilder.AppendLine("{");
            jsonBuilder.AppendLine($"  \"version\": \"{versionArg}\",");
            jsonBuilder.AppendLine("  \"dryRun\": true,");
            jsonBuilder.AppendLine($"  \"generatedAtUtc\": \"{DateTimeOffset.UtcNow:O}\",");
            jsonBuilder.AppendLine("  \"configuration\": {");
            int configCount = 0;
            foreach (var kvp in configuration)
            {
                configCount++;
                var comma = configCount < configuration.Count ? "," : "";
                jsonBuilder.AppendLine($"    \"{kvp.Key}\": \"{kvp.Value.Replace("\\", "\\\\")}\"{comma}");
            }
            jsonBuilder.AppendLine("  },");
            jsonBuilder.AppendLine("  \"appliedConfigs\": [");
            for (int i = 0; i < appliedConfigs.Count; i++)
            {
                var cfg = appliedConfigs[i];
                jsonBuilder.AppendLine("    {");
                jsonBuilder.AppendLine($"      \"name\": \"{cfg.name}\",");
                jsonBuilder.AppendLine($"      \"path\": \"{cfg.path.Replace("\\", "\\\\")}\",");
                jsonBuilder.AppendLine($"      \"exists\": {cfg.exists.ToString().ToLower()},");
                jsonBuilder.AppendLine($"      \"description\": \"{cfg.description}\"");
                jsonBuilder.Append(i < appliedConfigs.Count - 1 ? "    },\n" : "    }\n");
            }
            jsonBuilder.AppendLine("  ],");
            jsonBuilder.AppendLine("  \"operations\": [");
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                jsonBuilder.AppendLine("    {");
                jsonBuilder.AppendLine($"      \"name\": \"{op.name}\",");
                jsonBuilder.AppendLine($"      \"source\": \"{op.source.Replace("\\", "\\\\")}\",");
                jsonBuilder.AppendLine($"      \"target\": \"{op.target.Replace("\\", "\\\\")}\",");
                jsonBuilder.AppendLine($"      \"success\": {op.success.ToString().ToLower()}");
                jsonBuilder.Append(i < operations.Count - 1 ? "    },\n" : "    }\n");
            }
            jsonBuilder.AppendLine("  ]");
            jsonBuilder.Append("}");
            File.WriteAllText(jsonPath, jsonBuilder.ToString());

            // Build Markdown with configuration, applied configs, and operations
            var md = new System.Text.StringBuilder();
            md.AppendLine("# Build Information (Dry Run)");
            md.AppendLine();
            md.AppendLine("## Summary");
            md.AppendLine();
            md.AppendLine($"- **Version:** {versionArg}");
            md.AppendLine($"- **Dry Run:** true");
            md.AppendLine($"- **Generated At (UTC):** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}");
            md.AppendLine();
            md.AppendLine("## Configuration");
            md.AppendLine();
            foreach (var kvp in configuration)
            {
                md.AppendLine($"- **{kvp.Key}:** `{kvp.Value}`");
            }
            md.AppendLine();
            md.AppendLine("## Applied Preparation Configs");
            md.AppendLine();
            if (appliedConfigs.Count > 0)
            {
                foreach (var cfg in appliedConfigs)
                {
                    var status = cfg.exists ? "✅" : "❌";
                    md.AppendLine($"### {status} {cfg.name}");
                    md.AppendLine();
                    md.AppendLine($"- **Path:** `{cfg.path}`");
                    md.AppendLine($"- **Description:** {cfg.description}");
                    md.AppendLine($"- **Status:** {(cfg.exists ? "Found" : "Not Found")}");
                    md.AppendLine();
                }
            }
            else
            {
                md.AppendLine("*No preparation configs applied*");
                md.AppendLine();
            }
            md.AppendLine("## Operations");
            md.AppendLine();
            foreach (var op in operations)
            {
                var status = op.success ? "✅ Success" : "❌ Not Ready";
                md.AppendLine($"### {op.name} - {status}");
                md.AppendLine();
                md.AppendLine($"- **Source:** `{op.source}`");
                md.AppendLine($"- **Target:** `{op.target}`");
                md.AppendLine($"- **Status:** {(op.success ? "Ready" : "Pending")}");
                md.AppendLine();
            }
            File.WriteAllText(mdPath, md.ToString());

            Serilog.Log.Information("DRY-RUN report documents written: {Json} | {Md}", jsonPath, mdPath);
        });

    Target PreviewCodePatches => _ => _
        .Description("Generate code patch preview showing before/after content for all patches in injection config")
        .Executes(() =>
        {
            // Use the actual PreparationConfig parameter
            var prepConfigPath = ActualPreparationConfig;
            var configName = System.IO.Path.GetFileName(prepConfigPath.ToString());

            var previewOutput = ReportDocumentsOutput / "code-patches-preview.md";
            Directory.CreateDirectory(ReportDocumentsOutput);

            var md = new System.Text.StringBuilder();
            md.AppendLine("# Code Patches Preview");
            md.AppendLine();
            md.AppendLine($"**Generated:** {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            md.AppendLine();
            md.AppendLine($"**Config:** {configName}");
            md.AppendLine();

            if (!File.Exists(prepConfigPath))
            {
                Serilog.Log.Warning("Config not found: {Config}", configName);
                md.AppendLine("❌ **Config file not found**");
                File.WriteAllText(previewOutput, md.ToString());
                return;
            }

            try
            {
                var prepConfigJson = File.ReadAllText(prepConfigPath);
                var prepConfig = System.Text.Json.JsonDocument.Parse(prepConfigJson);
                var root = prepConfig.RootElement;

                if (!root.TryGetProperty("codePatches", out var codePatches))
                {
                    md.AppendLine("ℹ️ **No code patches defined in this config**");
                    File.WriteAllText(previewOutput, md.ToString());
                    Serilog.Log.Information("No code patches found in {Config}", configName);
                    return;
                }

                md.AppendLine($"## Patches from: {configName}");
                md.AppendLine();

                foreach (var filePatch in codePatches.EnumerateArray())
                {
                    var file = filePatch.TryGetProperty("file", out var f) ? f.GetString() : "";
                    var type = filePatch.TryGetProperty("type", out var t) ? t.GetString() : "Text";
                    var filePath = RepoRoot / file;

                    md.AppendLine($"### File: `{file}`");
                    md.AppendLine();
                    md.AppendLine($"**Type:** {type}");
                    md.AppendLine();

                    if (!File.Exists(filePath))
                    {
                        md.AppendLine("❌ **File not found**");
                        md.AppendLine();
                        continue;
                    }

                    var originalContent = File.ReadAllText(filePath);

                    if (filePatch.TryGetProperty("patches", out var patches))
                    {
                        var patchIndex = 0;
                        foreach (var patch in patches.EnumerateArray())
                        {
                            patchIndex++;
                                var operation = patch.TryGetProperty("operation", out var op) ? op.GetString() : "";
                                var mode = patch.TryGetProperty("mode", out var m) ? m.GetString() : "";
                                var search = patch.TryGetProperty("search", out var s) ? s.GetString() : "";
                                var replace = patch.TryGetProperty("replace", out var r) ? r.GetString() : "";
                            var description = patch.TryGetProperty("description", out var d) ? d.GetString() : "";

                            md.AppendLine($"#### Patch #{patchIndex}");
                            md.AppendLine();
                            if (!string.IsNullOrEmpty(description))
                            {
                                md.AppendLine($"**Description:** {description}");
                                md.AppendLine();
                            }
                                if (!string.IsNullOrEmpty(operation))
                                {
                                md.AppendLine($"**Operation:** `{operation}`");
                            }
                            if (!string.IsNullOrEmpty(mode))
                            {
                                md.AppendLine($"**Mode:** `{mode}`");
                            }
                            md.AppendLine($"**Search Pattern:** `{search}`");
                            if (!string.IsNullOrEmpty(replace))
                            {
                                md.AppendLine($"**Replace With:** `{replace}`");
                            }
                                md.AppendLine();
                            }
                        }

                    md.AppendLine("**Original Content:**");
                    md.AppendLine();
                    md.AppendLine("```" + (type == "CSharp" ? "csharp" : type == "Json" ? "json" : ""));
                    md.AppendLine(originalContent);
                    md.AppendLine("```");
                    md.AppendLine();

                    // Simulate patched content for preview (simple text replacement)
                    var patchedContent = originalContent;
                    var patchApplied = false;

                    if (filePatch.TryGetProperty("patches", out var patchesForSimulation))
                    {
                        foreach (var patch in patchesForSimulation.EnumerateArray())
                        {
                                var operation = patch.TryGetProperty("operation", out var op) ? op.GetString() : "";
                                var mode = patch.TryGetProperty("mode", out var m) ? m.GetString() : "";
                                var search = patch.TryGetProperty("search", out var s) ? s.GetString() : "";
                                var replace = patch.TryGetProperty("replace", out var r) ? r.GetString() : "";

                            // For Roslyn operations, show a note that actual result may differ
                            if (!string.IsNullOrEmpty(operation))
                            {
                                // Can't simulate Roslyn operations accurately, just show a note
                                continue;
                            }

                            // For text-based patches, simulate the result
                            if (!string.IsNullOrEmpty(search))
                            {
                                if (mode == "Delete" || string.IsNullOrEmpty(replace))
                                {
                                    patchedContent = patchedContent.Replace(search, string.Empty);
                                    patchApplied = true;
                                }
                                else
                                {
                                    patchedContent = patchedContent.Replace(search, replace);
                                    patchApplied = true;
                                }
                            }
                        }
                    }

                    if (patchApplied || type == "CSharp")
                    {
                        md.AppendLine("**Modified Content (After Patches):**");
                        md.AppendLine();

                        if (type == "CSharp" && !patchApplied)
                        {
                            md.AppendLine("_Note: Roslyn-based patches require actual compilation to show accurate results._");
                            md.AppendLine("_The build tool will apply these patches using syntax tree manipulation._");
                            md.AppendLine();
                            md.AppendLine("**Expected changes:**");
                            md.AppendLine();
                            if (filePatch.TryGetProperty("patches", out var roslynPatches))
                            {
                                foreach (var patch in roslynPatches.EnumerateArray())
                                {
                                    var operation = patch.TryGetProperty("operation", out var op) ? op.GetString() : "";
                                    var search = patch.TryGetProperty("search", out var s) ? s.GetString() : "";
                                    md.AppendLine($"- **{operation}**: All statements containing `{search}` will be removed");
                                }
                            }
                            md.AppendLine();
                        }
                        else
                        {
                            md.AppendLine("```" + (type == "CSharp" ? "csharp" : type == "Json" ? "json" : ""));
                            md.AppendLine(patchedContent);
                            md.AppendLine("```");
                            md.AppendLine();
                        }
                    }

                    md.AppendLine("---");
                    md.AppendLine();
                }
            }
            catch (System.Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to generate preview for {Config}", configName);
                md.AppendLine();
                md.AppendLine($"❌ **Error generating preview:** {ex.Message}");
            }

            File.WriteAllText(previewOutput, md.ToString());
            Serilog.Log.Information("Code patches preview written: {Path}", previewOutput);
        });
}
