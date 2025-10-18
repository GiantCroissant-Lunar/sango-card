using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SangoCard.Game.Report
{
    public static class SangoReportRuntime
    {
        public static async Task<(string? jsonPath, string? mdPath)> GenerateAndSaveAsync<T>(
            PreparedModel<T> model,
            ReportFormat formats,
            string? directory = null,
            string? fileBaseName = null,
            CancellationToken ct = default)
        {
            directory ??= Path.Combine(Application.persistentDataPath, "Reports");
            Directory.CreateDirectory(directory);

            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            fileBaseName ??= string.IsNullOrWhiteSpace(model?.Metadata?.Id) ? "report" : model.Metadata.Id;
            var baseName = $"{Sanitize(fileBaseName)}-{stamp}";

            string? jsonPath = null;
            string? mdPath = null;

            if (formats.HasFlag(ReportFormat.Json))
            {
                var json = JsonSerializer.Serialize(model, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                jsonPath = Path.Combine(directory, baseName + ".json");
                await File.WriteAllTextAsync(jsonPath, json, ct);
            }

            if (formats.HasFlag(ReportFormat.Markdown))
            {
                var md = RenderMarkdown(model);
                mdPath = Path.Combine(directory, baseName + ".md");
                await File.WriteAllTextAsync(mdPath, md, ct);
            }

            return (jsonPath, mdPath);
        }

        private static string RenderMarkdown<T>(PreparedModel<T> model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# " + Escape(model.Metadata?.Title ?? model.Metadata?.Id ?? "Report"));
            sb.AppendLine();
            if (!string.IsNullOrEmpty(model.Metadata?.Description))
            {
                sb.AppendLine(Escape(model.Metadata.Description));
                sb.AppendLine();
            }
            sb.AppendLine("## Report Information");
            sb.AppendLine();
            sb.AppendLine($"- **Report ID:** {Escape(model.Metadata?.Id)}");
            sb.AppendLine($"- **Generated At:** {(model.PreparedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"))} UTC");
            sb.AppendLine();
            sb.AppendLine("## Data");
            sb.AppendLine();

            if (model.Data is null)
            {
                sb.AppendLine("*No data available.*");
                return sb.ToString();
            }

            if (model.Data is IEnumerable enumerable and not string)
            {
                var list = enumerable.Cast<object>().ToList();
                WriteTable(sb, list, model.Metadata?.Columns);
            }
            else
            {
                WriteTable(sb, new List<object> { model.Data! }, model.Metadata?.Columns);
            }

            return sb.ToString();
        }

        private static void WriteTable(StringBuilder sb, List<object> items, IReadOnlyList<ReportColumnInfo>? columns)
        {
            if (items.Count == 0)
            {
                sb.AppendLine("*No data rows found.*");
                return;
            }

            var cols = (columns ?? Array.Empty<ReportColumnInfo>())
                .OrderBy(c => c.SortOrder ?? int.MaxValue)
                .ThenBy(c => c.DisplayName)
                .ToList();

            if (cols.Count == 0)
            {
                var props = items[0].GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanRead)
                    .ToArray();
                cols = props.Select(p => new ReportColumnInfo { PropertyName = p.Name, DisplayName = p.Name }).ToList();
            }

            sb.Append("| ");
            sb.Append(string.Join(" | ", cols.Select(c => Escape(c.DisplayName))));
            sb.AppendLine(" |");
            sb.Append("| ");
            sb.Append(string.Join(" | ", cols.Select(_ => "---")));
            sb.AppendLine(" |");

            foreach (var it in items)
            {
                sb.Append("| ");
                var values = cols.Select(c =>
                {
                    var prop = it.GetType().GetProperty(c.PropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (prop == null || !prop.CanRead) return string.Empty;
                    var val = prop.GetValue(it);
                    return Escape(FormatValue(val, c.Format));
                });
                sb.Append(string.Join(" | ", values));
                sb.AppendLine(" |");
            }
        }

        private static string FormatValue(object? value, string? format)
        {
            if (value == null) return string.Empty;
            if (!string.IsNullOrEmpty(format))
            {
                try
                {
                    return value switch
                    {
                        DateTime dt => dt.ToString(format),
                        DateTimeOffset dto => dto.ToString(format),
                        IFormattable formattable => formattable.ToString(format, null),
                        _ => value.ToString() ?? string.Empty
                    };
                }
                catch { }
            }
            return value.ToString() ?? string.Empty;
        }

        private static string Escape(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text
                .Replace("\\", "\\\\")
                .Replace("|", "\\|")
                .Replace("*", "\\*")
                .Replace("_", "\\_")
                .Replace("`", "\\`")
                .Replace("#", "\\#");
        }

        private static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '-');
            return value;
        }
    }
}
