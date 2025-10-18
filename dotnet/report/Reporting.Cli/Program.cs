using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Reporting.Abstractions;
using Reporting.Core;

namespace Reporting.Cli;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var outputOption = new Option<string>(name: "--output", description: "Output directory for report files") { IsRequired = true };
        var artifactVersionOption = new Option<string>(name: "--artifacts-version", description: "Artifacts version label to include in filenames") { IsRequired = false };
        var formatsOption = new Option<string[]>(name: "--formats", description: "Formats to emit (json, md)") { IsRequired = false, Arity = ArgumentArity.ZeroOrMore };
        var dryRunOption = new Option<bool>(name: "--dry-run", getDefaultValue: () => false, description: "Include dry-run flag and suffix outputs");
        var nameOption = new Option<string>(name: "--name", getDefaultValue: () => "build-info", description: "Base filename without extension");

        var root = new RootCommand("SangoCard Reporting CLI");
        root.AddOption(outputOption);
        root.AddOption(artifactVersionOption);
        root.AddOption(formatsOption);
        root.AddOption(nameOption);
        root.AddOption(dryRunOption);

        root.SetHandler(async (string output, string? version, string[]? formats, string name, bool dryRun) =>
        {
            var ct = CancellationToken.None;
            Directory.CreateDirectory(output);

            // Decide formats
            var fmt = ReportFormat.None;
            if (formats == null || formats.Length == 0)
            {
                fmt = ReportFormat.Json | ReportFormat.Markdown;
            }
            else
            {
                foreach (var f in formats.Select(f => f?.Trim().ToLowerInvariant()).Where(f => !string.IsNullOrEmpty(f)))
                {
                    if (f == "json") fmt |= ReportFormat.Json;
                    else if (f == "md" || f == "markdown") fmt |= ReportFormat.Markdown;
                }
                if (fmt == ReportFormat.None)
                {
                    fmt = ReportFormat.Json | ReportFormat.Markdown;
                }
            }

            // Provider: basic build info
            var provider = new BuildInfoProvider(version, dryRun);
            var engine = new SimpleReportEngine();
            var result = await engine.GenerateAsync(provider, new ReportOptions { Formats = fmt }, ct);

            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileBase = string.IsNullOrWhiteSpace(version) ? name : $"{name}-{Sanitize(version)}";
            if (dryRun) fileBase += "-dryrun";
            fileBase = $"{fileBase}-{stamp}";

            if (result.Json is { Length: > 0 } && fmt.HasFlag(ReportFormat.Json))
            {
                var jsonPath = Path.Combine(output, fileBase + ".json");
                await File.WriteAllTextAsync(jsonPath, result.Json, ct);
                Console.WriteLine($"Wrote {jsonPath}");
            }
            if (result.Markdown is { Length: > 0 } && fmt.HasFlag(ReportFormat.Markdown))
            {
                var mdPath = Path.Combine(output, fileBase + ".md");
                await File.WriteAllTextAsync(mdPath, result.Markdown, ct);
                Console.WriteLine($"Wrote {mdPath}");
            }
        }, outputOption, artifactVersionOption, formatsOption, nameOption, dryRunOption);

        return await root.InvokeAsync(args);
    }

    private static string Sanitize(string value)
        => string.Join('-', value.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private sealed class BuildInfoProvider : IReportDataProvider<BuildInfo>
    {
        private readonly string? _version;
        private readonly bool _dryRun;
        public BuildInfoProvider(string? version, bool dryRun)
        {
            _version = version;
            _dryRun = dryRun;
        }

        public Task<PreparedModel<BuildInfo>> GetDataAsync(CancellationToken ct = default)
        {
            var data = new BuildInfo
            {
                Version = _version ?? "local",
                DryRun = _dryRun,
                Branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME")
                    ?? Environment.GetEnvironmentVariable("BUILD_SOURCEBRANCHNAME")
                    ?? TryGit("rev-parse --abbrev-ref HEAD"),
                Commit = Environment.GetEnvironmentVariable("GITHUB_SHA")
                    ?? Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION")
                    ?? TryGit("rev-parse --short HEAD"),
                Machine = Environment.MachineName,
                Os = Environment.OSVersion.ToString(),
                GeneratedAt = DateTimeOffset.UtcNow
            };

            var model = new PreparedModel<BuildInfo>
            {
                Data = data,
                PreparedAt = DateTimeOffset.UtcNow,
                Metadata = new ReportMetadata
                {
                    Id = "build-info",
                    Title = "Build Information",
                    Description = "Versioned build metadata for SangoCard",
                    Columns = new[]
                    {
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.Version), DisplayName = "Version" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.DryRun), DisplayName = "Dry Run" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.Branch), DisplayName = "Branch" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.Commit), DisplayName = "Commit" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.Machine), DisplayName = "Machine" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.Os), DisplayName = "OS" },
                        new ReportColumnInfo{ PropertyName = nameof(BuildInfo.GeneratedAt), DisplayName = "Generated At", Format = "yyyy-MM-dd HH:mm:ss" }
                    }
                }
            };

            return Task.FromResult(model);
        }

        public ReportMetadata GetMetadata() => new()
        {
            Id = "build-info",
            Title = "Build Information"
        };

        private static string TryGit(string args)
        {
            try
            {
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                process?.WaitForExit(3000);
                var output = process?.StandardOutput.ReadToEnd()?.Trim();
                return string.IsNullOrWhiteSpace(output) ? "unknown" : output;
            }
            catch
            {
                return "unknown";
            }
        }
    }

    private sealed class BuildInfo
    {
        public string? Version { get; set; }
        public bool DryRun { get; set; }
        public string? Branch { get; set; }
        public string? Commit { get; set; }
        public string? Machine { get; set; }
        public string? Os { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
    }
}
