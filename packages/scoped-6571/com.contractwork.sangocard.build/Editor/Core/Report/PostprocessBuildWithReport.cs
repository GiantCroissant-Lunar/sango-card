using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SangoCard.Build.Editor.Report;

internal partial class PostprocessBuildWithReport : IPostprocessBuildWithReport
{
#pragma warning disable IDE1006
    public int callbackOrder => 0;
#pragma warning restore IDE1006

    public void OnPostprocessBuild(BuildReport report)
    {
        try
        {
            // Determine repo root (two levels up from Assets)
            var assets = Application.dataPath; // <repo>/projects/client/Assets
            var repoRoot = Path.GetFullPath(Path.Combine(assets, "..", ".."));

            // Resolve version from command line args (passed by NUKE as --buildVersion)
            var version = ResolveBuildVersionFromArgs() ?? "local";

            // Output directories based on agreed layout
            var reportDir = Path.Combine(repoRoot, "build", "_artifacts", version, "build", "report");
            Directory.CreateDirectory(reportDir);

            // Prepare minimal JSON/Markdown
            var ts = System.DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var baseName = $"unity-build-summary-{ts}";

            var jsonPath = Path.Combine(reportDir, baseName + ".json");
            var mdPath = Path.Combine(reportDir, baseName + ".md");

            var sum = report.summary;
            var json =
                "{" +
                $"\n  \"version\": \"{Escape(version)}\"," +
                $"\n  \"platform\": \"{sum.platform}\"," +
                $"\n  \"result\": \"{sum.result}\"," +
                $"\n  \"outputPath\": \"{Escape(sum.outputPath)}\"," +
                $"\n  \"totalSize\": {sum.totalSize}," +
                $"\n  \"totalTimeSeconds\": {sum.totalTime.TotalSeconds:0.###}," +
                $"\n  \"totalErrors\": {sum.totalErrors}," +
                $"\n  \"totalWarnings\": {sum.totalWarnings}" +
                "\n}";

            File.WriteAllText(jsonPath, json);

            var md = new System.Text.StringBuilder();
            md.AppendLine("# Unity Build Summary");
            md.AppendLine();
            md.AppendLine($"- **Version:** {Escape(version)}");
            md.AppendLine($"- **Platform:** {sum.platform}");
            md.AppendLine($"- **Result:** {sum.result}");
            md.AppendLine($"- **Output Path:** {Escape(sum.outputPath)}");
            md.AppendLine($"- **Total Size (bytes):** {sum.totalSize}");
            md.AppendLine($"- **Total Time (s):** {sum.totalTime.TotalSeconds:0.###}");
            md.AppendLine($"- **Errors:** {sum.totalErrors}");
            md.AppendLine($"- **Warnings:** {sum.totalWarnings}");
            md.AppendLine();
            md.AppendLine("## Steps");
            foreach (var step in report.steps)
            {
                md.AppendLine($"- {Escape(step.name)} ({step.duration.TotalSeconds:0.###} s)");
            }
            File.WriteAllText(mdPath, md.ToString());

            Debug.Log($"[Report] Wrote build reports:\n  - {jsonPath}\n  - {mdPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Report] Failed to write build reports: {ex.Message}");
        }
    }

    private static string? ResolveBuildVersionFromArgs()
    {
        try
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                var token = args[i];
                if (!token.StartsWith("--")) continue;
                var trimmed = token.TrimStart('-');
                string key;
                string? value;
                var eq = trimmed.IndexOf('=');
                if (eq >= 0)
                {
                    key = trimmed.Substring(0, eq);
                    value = trimmed.Substring(eq + 1);
                }
                else
                {
                    key = trimmed;
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        value = args[i + 1];
                    else
                        value = null;
                }
                if (string.Equals(key, "buildVersion", System.StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(value))
                    return value;
            }
        }
        catch { }
        return null;
    }

    private static string Escape(string? s)
        => string.IsNullOrEmpty(s) ? string.Empty : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
